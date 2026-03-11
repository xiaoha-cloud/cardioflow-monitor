#!/usr/bin/env python3
"""
MIT-BIH Data Replay to Kafka

This script loads MIT-BIH ECG records and replays them to Kafka in real-time
(or at a specified speed). It sends telemetry messages to the ecg.telemetry topic.

Usage:
    python replay.py --record 100 --limit 1000 --speed 1.0
"""

import argparse
import logging
import sys
import time
from datetime import datetime
from typing import Optional
import numpy as np
import os
from dotenv import load_dotenv

# Import functions from main.py
from main import load_record, parse_annotations
from producer import KafkaTelemetryProducer, create_telemetry_message, find_annotation_at_sample

# Load environment variables from .env file (if present)
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(levelname)s] %(message)s',
    handlers=[logging.StreamHandler(sys.stdout)]
)
logger = logging.getLogger(__name__)


def calculate_heart_rate(
    sample_index: int,
    annotations: list,
    fs: int
) -> Optional[int]:
    """
    Calculate heart rate based on adjacent Normal beats.
    
    This function finds the previous and next Normal ('N') beats around the
    current sample and calculates heart rate from their interval.
    
    Heart Rate Calculation:
    - Find two consecutive Normal beats
    - Calculate time difference: delta_t = (sample2 - sample1) / fs
    - Heart rate = 60 / delta_t (beats per minute)
    
    Args:
        sample_index: Current sample position
        annotations: List of annotation dictionaries
        fs: Sampling rate in Hz
        
    Returns:
        Heart rate in bpm, or None if cannot be calculated
    """
    # Find previous Normal beat
    prev_n = None
    for ann in reversed(annotations):
        if ann['sample_index'] <= sample_index and ann['symbol'] == 'N':
            prev_n = ann
            break
    
    # Find next Normal beat
    next_n = None
    for ann in annotations:
        if ann['sample_index'] > sample_index and ann['symbol'] == 'N':
            next_n = ann
            break
    
    # Calculate heart rate from interval
    if prev_n and next_n:
        interval_samples = next_n['sample_index'] - prev_n['sample_index']
        interval_seconds = interval_samples / fs
        if interval_seconds > 0:
            heart_rate = int(60.0 / interval_seconds)
            # Clamp to reasonable range (30-200 bpm)
            heart_rate = max(30, min(200, heart_rate))
            return heart_rate
    
    # Fallback: use fixed value if cannot calculate
    return 72  # Default heart rate


def replay_to_kafka(
    record_id: str,
    limit: Optional[int] = None,
    speed: float = 1.0,
    bootstrap_servers: str = 'localhost:9092'
) -> None:
    """
    Replay MIT-BIH ECG data to Kafka.
    
    This function:
    1. Loads the MIT-BIH record and annotations
    2. Initializes Kafka producer
    3. Sends samples to Kafka at the specified rate
    
    Time Interval Calculation:
    - Sampling rate: 360 Hz (MIT-BIH standard)
    - Base interval: 1 / 360 ≈ 0.002778 seconds per sample
    - Adjusted interval: base_interval / speed_multiplier
    
    Args:
        record_id: MIT-BIH record identifier (e.g., '100')
        limit: Maximum number of samples to send (None = all samples)
        speed: Replay speed multiplier (1.0 = real-time, 2.0 = 2x speed)
        bootstrap_servers: Kafka bootstrap servers address
    """
    producer = None
    
    try:
        # Load ECG data
        logger.info(f"Loading MIT-BIH record {record_id}...")
        signals, fs = load_record(record_id)
        
        # Extract lead1 data (first channel)
        lead1_data = signals[:, 0]
        num_samples = len(lead1_data)
        
        # Parse annotations
        logger.info(f"Parsing annotations for record {record_id}...")
        annotations = parse_annotations(record_id)
        
        # Calculate timestamps for annotations (for heart rate calculation)
        for ann in annotations:
            ann['timestamp'] = ann['sample_index'] / fs
        
        # Determine number of samples to send
        samples_to_send = limit if limit is not None else num_samples
        samples_to_send = min(samples_to_send, num_samples)
        
        logger.info(f"Total samples in record: {num_samples:,}")
        logger.info(f"Samples to send: {samples_to_send:,}")
        logger.info(f"Replay speed: {speed}x")
        
        # Initialize Kafka producer
        logger.info(f"Connecting to Kafka at {bootstrap_servers}...")
        producer = KafkaTelemetryProducer(bootstrap_servers=bootstrap_servers)
        logger.info("Kafka producer connected successfully")
        
        # Calculate time interval between samples
        # Base interval = 1 / sampling_rate (seconds per sample)
        base_interval = 1.0 / fs
        # Adjusted interval based on speed multiplier
        interval = base_interval / speed
        
        logger.info(f"Starting replay of record {record_id}...")
        logger.info(f"Sample interval: {interval*1000:.3f} ms (base: {base_interval*1000:.3f} ms)")
        logger.info(f"Sending {samples_to_send} samples...")
        
        # Track statistics
        start_time = time.time()
        last_log_time = start_time
        messages_sent = 0
        
        # Replay loop
        for i in range(samples_to_send):
            sample_index = i
            lead1_value = float(lead1_data[sample_index])
            
            # Find annotation for this sample
            annotation_symbol = find_annotation_at_sample(sample_index, annotations)
            
            # Calculate heart rate (simplified - uses fixed value for now)
            # In production, you might want more sophisticated calculation
            heart_rate = calculate_heart_rate(sample_index, annotations, fs)
            
            # Create timestamp (current time for replay)
            timestamp = datetime.utcnow()
            
            # Create telemetry message
            message = create_telemetry_message(
                record_id=record_id,
                sample_index=sample_index,
                lead1_value=lead1_value,
                annotation=annotation_symbol,
                timestamp=timestamp,
                heart_rate=heart_rate
            )
            
            # Send to Kafka
            success = producer.send_telemetry(message)
            
            if success:
                messages_sent += 1
            else:
                logger.warning(f"Failed to send sample {sample_index}")
            
            # Log progress every 100 samples
            if (i + 1) % 100 == 0:
                elapsed = time.time() - start_time
                rate = (i + 1) / elapsed if elapsed > 0 else 0
                logger.info(f"Sent sample {i+1}/{samples_to_send} "
                          f"({(i+1)*100/samples_to_send:.1f}%, "
                          f"rate: {rate:.1f} samples/sec)")
            
            # Wait for next sample (except for the last one)
            if i < samples_to_send - 1:
                time.sleep(interval)
        
        # Flush remaining messages
        logger.info("Flushing remaining messages...")
        producer.flush()
        
        # Calculate final statistics
        total_time = time.time() - start_time
        avg_rate = messages_sent / total_time if total_time > 0 else 0
        
        logger.info("=" * 60)
        logger.info("Replay completed successfully")
        logger.info(f"Messages sent: {messages_sent:,}")
        logger.info(f"Total time: {total_time:.2f} seconds")
        logger.info(f"Average rate: {avg_rate:.1f} messages/second")
        logger.info("=" * 60)
        
    except KeyboardInterrupt:
        logger.warning("Replay interrupted by user")
        if producer:
            logger.info("Flushing remaining messages...")
            producer.flush()
    except Exception as e:
        logger.error(f"Error during replay: {e}", exc_info=True)
        raise
    finally:
        # Ensure producer is closed
        if producer:
            producer.close()


def main():
    """
    Main function with command-line argument parsing.
    """
    parser = argparse.ArgumentParser(
        description='Replay MIT-BIH ECG data to Kafka',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Replay all samples at real-time speed
  python replay.py --record 100
  
  # Replay first 1000 samples for testing
  python replay.py --record 100 --limit 1000
  
  # Replay at 2x speed
  python replay.py --record 100 --speed 2.0
  
  # Use custom Kafka server
  python replay.py --record 100 --bootstrap-servers localhost:9093
        """
    )
    
    parser.add_argument(
        '--record',
        type=str,
        default='100',
        help='MIT-BIH record ID (default: 100)'
    )
    
    parser.add_argument(
        '--limit',
        type=int,
        default=None,
        help='Maximum number of samples to send (default: all samples)'
    )
    
    parser.add_argument(
        '--speed',
        type=float,
        default=1.0,
        help='Replay speed multiplier (default: 1.0 = real-time)'
    )
    
    parser.add_argument(
        '--bootstrap-servers',
        type=str,
        default=None,
        help='Kafka bootstrap servers (default: from KAFKA_BOOTSTRAP_SERVERS env var or localhost:9092)'
    )
    
    args = parser.parse_args()
    
    # Determine bootstrap servers
    bootstrap_servers = (
        args.bootstrap_servers or
        os.getenv('KAFKA_BOOTSTRAP_SERVERS') or
        'localhost:9092'
    )
    
    # Validate arguments
    if args.speed <= 0:
        logger.error("Speed must be greater than 0")
        sys.exit(1)
    
    if args.limit is not None and args.limit <= 0:
        logger.error("Limit must be greater than 0")
        sys.exit(1)
    
    # Run replay
    try:
        replay_to_kafka(
            record_id=args.record,
            limit=args.limit,
            speed=args.speed,
            bootstrap_servers=bootstrap_servers
        )
    except Exception as e:
        logger.error(f"Replay failed: {e}")
        sys.exit(1)


if __name__ == '__main__':
    main()

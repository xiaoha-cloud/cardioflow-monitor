#!/usr/bin/env python3
"""
Kafka Producer Module for CardioFlow Monitor

This module provides functionality to send ECG telemetry data to Kafka topics.
It handles message serialization, error handling, and connection management.
"""

import json
import logging
from datetime import datetime
from typing import Dict, Optional, Any
from confluent_kafka import Producer, KafkaError, KafkaException

logger = logging.getLogger(__name__)


class KafkaTelemetryProducer:
    """
    Kafka producer for sending ECG telemetry messages.
    
    This class manages the connection to Kafka and provides methods to send
    telemetry data to the ecg.telemetry topic.
    
    Configuration:
    - bootstrap.servers: Kafka broker address (default: localhost:9092)
    - client.id: Client identifier for logging
    - acks: Acknowledgment level (1 = wait for leader, all = wait for all replicas)
    - retries: Number of retry attempts on failure
    """
    
    def __init__(self, bootstrap_servers: str = 'localhost:9092'):
        """
        Initialize Kafka producer.
        
        Args:
            bootstrap_servers: Kafka broker address (e.g., 'localhost:9092')
            
        Raises:
            KafkaException: If producer initialization fails
        """
        try:
            config = {
                'bootstrap.servers': bootstrap_servers,
                'client.id': 'cardioflow-simulator',
                'acks': '1',  # Wait for leader acknowledgment
                'retries': 3,
                'retry.backoff.ms': 100,
                'compression.type': 'none',  # No compression for small messages
                'max.in.flight.requests.per.connection': 5
            }
            
            self.producer = Producer(config)
            self.bootstrap_servers = bootstrap_servers
            self.topic = 'ecg.telemetry'
            
            logger.info(f"Kafka producer initialized (bootstrap_servers={bootstrap_servers})")
            
        except Exception as e:
            logger.error(f"Failed to initialize Kafka producer: {e}")
            raise KafkaException(f"Producer initialization failed: {e}")
    
    def send_telemetry(self, message_dict: Dict[str, Any]) -> bool:
        """
        Send a telemetry message to Kafka.
        
        The message is serialized as JSON and sent to the ecg.telemetry topic.
        This method is asynchronous - it queues the message and returns immediately.
        Use flush() to ensure all messages are delivered.
        
        Args:
            message_dict: Dictionary containing telemetry data
            
        Returns:
            True if message was queued successfully, False otherwise
        """
        try:
            # Serialize message to JSON
            message_json = json.dumps(message_dict, ensure_ascii=False)
            
            # Send to Kafka (asynchronous)
            self.producer.produce(
                self.topic,
                value=message_json.encode('utf-8'),
                callback=self._delivery_callback
            )
            
            # Trigger delivery callbacks for queued messages
            # This is non-blocking and handles delivery reports
            self.producer.poll(0)
            
            return True
            
        except BufferError as e:
            logger.error(f"Producer queue is full: {e}")
            logger.error("Consider increasing queue size or reducing send rate")
            return False
        except Exception as e:
            logger.error(f"Failed to send message: {e}")
            return False
    
    def _delivery_callback(self, err, msg):
        """
        Callback function for message delivery reports.
        
        This is called asynchronously when Kafka confirms message delivery
        or reports an error. Used for error tracking and debugging.
        
        Args:
            err: KafkaError if delivery failed, None if successful
            msg: Message object with delivery metadata
        """
        if err is not None:
            logger.error(f"Message delivery failed: {err}")
        else:
            # Message delivered successfully (optional debug logging)
            logger.debug(f"Message delivered to {msg.topic()} [{msg.partition()}] at offset {msg.offset()}")
    
    def flush(self, timeout: float = 10.0) -> None:
        """
        Wait for all pending messages to be delivered.
        
        This method blocks until all queued messages are sent or timeout occurs.
        Should be called before closing the producer to ensure all messages are delivered.
        
        Args:
            timeout: Maximum time to wait in seconds
        """
        try:
            remaining = self.producer.flush(timeout)
            if remaining > 0:
                logger.warning(f"{remaining} messages were not delivered before timeout")
            else:
                logger.debug("All messages delivered successfully")
        except Exception as e:
            logger.error(f"Error flushing producer: {e}")
    
    def close(self) -> None:
        """
        Close the Kafka producer connection.
        
        This method flushes all pending messages and closes the connection.
        Should be called when done sending messages to ensure proper cleanup.
        """
        try:
            logger.info("Closing Kafka producer...")
            self.flush()
            self.producer = None
            logger.info("Kafka producer closed successfully")
        except Exception as e:
            logger.error(f"Error closing producer: {e}")


def create_telemetry_message(
    record_id: str,
    sample_index: int,
    lead1_value: float,
    annotation: str,
    timestamp: datetime,
    heart_rate: Optional[int] = None
) -> Dict[str, Any]:
    """
    Create a telemetry message dictionary in the CardioFlow format.
    
    Telemetry Message Format:
    {
        "patientId": "mitdb-{record_id}",  # Patient identifier
        "recordId": "{record_id}",          # MIT-BIH record ID
        "deviceId": "ecg-sim-01",           # Simulator device ID
        "timestamp": "ISO8601",             # ISO 8601 timestamp
        "sampleIndex": int,                 # Sample position in record
        "lead1": float,                     # ECG lead1 value (mV)
        "annotation": "N|V|A|...",         # Beat annotation symbol
        "heartRate": int,                   # Heart rate in bpm (optional)
        "status": "normal|abnormal",        # Overall status
        "signalQuality": "good",            # Signal quality indicator
        "battery": int                      # Battery level (simulated)
    }
    
    Args:
        record_id: MIT-BIH record identifier (e.g., '100')
        sample_index: Sample position in the signal array
        lead1_value: ECG lead1 signal value
        annotation: Beat annotation symbol (N, V, A, etc.)
        timestamp: Timestamp for this sample
        heart_rate: Heart rate in bpm (optional, defaults to None)
        
    Returns:
        Dictionary containing formatted telemetry message
    """
    # Determine status based on annotation
    # Normal beats: N, L, R
    # Abnormal beats: V, A, E, F, etc.
    normal_annotations = {'N', 'L', 'R'}
    status = 'normal' if annotation in normal_annotations else 'abnormal'
    
    # Format timestamp as ISO 8601 with Z suffix (UTC)
    timestamp_str = timestamp.isoformat() + 'Z'
    
    # Create message dictionary
    message = {
        'patientId': f'mitdb-{record_id}',
        'recordId': record_id,
        'deviceId': 'ecg-sim-01',
        'timestamp': timestamp_str,
        'sampleIndex': sample_index,
        'lead1': round(lead1_value, 6),  # Round to 6 decimal places
        'annotation': annotation,
        'status': status,
        'signalQuality': 'good',
        'battery': 87  # Simulated battery level
    }
    
    # Add heart rate if provided
    if heart_rate is not None:
        message['heartRate'] = heart_rate
    
    return message


def find_annotation_at_sample(
    sample_index: int,
    annotations: list,
    default: str = 'N'
) -> str:
    """
    Find the annotation symbol for a given sample index.
    
    This function searches for an annotation at or near the specified sample index.
    If an exact match is found, returns that annotation's symbol.
    Otherwise, returns the default value ('N' for Normal).
    
    Args:
        sample_index: Sample position to search for
        annotations: List of annotation dictionaries, each containing 'sample_index' and 'symbol'
        default: Default annotation symbol if no match found (default: 'N')
        
    Returns:
        Annotation symbol (e.g., 'N', 'V', 'A')
    """
    # Search for exact match first
    for ann in annotations:
        if ann['sample_index'] == sample_index:
            return ann['symbol']
    
    # If no exact match, find the closest annotation before this sample
    # (This is a simplified approach - in practice, you might want more sophisticated matching)
    closest_ann = None
    min_distance = float('inf')
    
    for ann in annotations:
        distance = abs(ann['sample_index'] - sample_index)
        if ann['sample_index'] <= sample_index and distance < min_distance:
            min_distance = distance
            closest_ann = ann
    
    # If we found a close annotation within a reasonable window (e.g., 50 samples)
    if closest_ann and min_distance < 50:
        return closest_ann['symbol']
    
    # Default to Normal if no annotation found
    return default

#!/usr/bin/env python3
"""
MIT-BIH Arrhythmia Database Replay Simulator

This module provides functionality to load and parse ECG records from the
MIT-BIH Arrhythmia Database. It reads signal data and annotations, providing
a foundation for real-time ECG monitoring simulation.

MIT-BIH Database Structure:
- Each record contains two-channel ECG signals sampled at 360 Hz
- Annotation files (.atr) contain beat-by-beat annotations with:
  - Sample index: position in the signal array
  - Symbol: annotation type (N=Normal, V=PVC, A=Atrial Premature, etc.)
  - Auxiliary information: additional metadata
"""

import logging
import sys
from typing import List, Tuple, Dict, Any
import numpy as np
import wfdb

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(levelname)s] %(message)s',
    handlers=[logging.StreamHandler(sys.stdout)]
)
logger = logging.getLogger(__name__)


def load_record(record_name: str) -> Tuple[np.ndarray, int]:
    """
    Load MIT-BIH record and return signal data and sampling rate.
    
    The MIT-BIH database stores ECG signals as two-channel recordings.
    This function loads the record and extracts the first channel (lead1).
    
    Args:
        record_name: MIT-BIH record identifier (e.g., '100')
        
    Returns:
        Tuple containing:
            - signals: 2D numpy array of shape (num_samples, num_channels)
            - fs: Sampling rate in Hz (typically 360 Hz for MIT-BIH)
            
    Raises:
        ConnectionError: If unable to download record from PhysioNet
        FileNotFoundError: If record cannot be found
        ValueError: If data format is invalid
    """
    try:
        logger.info(f"Loading MIT-BIH record {record_name}...")
        
        # Download and read the record
        # rdsamp downloads the record from PhysioNet if not cached locally
        signals, fields = wfdb.rdsamp(record_name, pn_dir='mitdb')
        
        # Extract sampling rate from fields dictionary
        fs = fields['fs']
        
        logger.info(f"Record loaded successfully.")
        logger.info(f"Signal shape: {signals.shape}, Sampling rate: {fs} Hz")
        
        return signals, fs
        
    except Exception as e:
        if 'Connection' in str(type(e).__name__) or 'network' in str(e).lower():
            logger.error(f"Network error: Unable to download record {record_name} from PhysioNet.")
            logger.error("Please check your internet connection and try again.")
            raise ConnectionError(f"Failed to download record: {e}")
        elif 'FileNotFound' in str(type(e).__name__) or 'not found' in str(e).lower():
            logger.error(f"Record {record_name} not found in MIT-BIH database.")
            raise FileNotFoundError(f"Record {record_name} not found: {e}")
        else:
            logger.error(f"Error loading record {record_name}: {e}")
            raise ValueError(f"Invalid data format: {e}")


def parse_annotations(record_name: str) -> List[Dict[str, Any]]:
    """
    Parse annotation file for a MIT-BIH record.
    
    MIT-BIH annotation files contain beat-by-beat annotations with:
    - Sample index: position in the signal where the beat occurs
    - Symbol: annotation type indicating the beat classification
    - Auxiliary info: additional metadata (not used in this basic version)
    
    Annotation Types (common):
    - N: Normal beat
    - V: Premature Ventricular Contraction (PVC) - abnormal ventricular beat
    - A: Atrial Premature beat - early atrial contraction
    - L: Left bundle branch block beat
    - R: Right bundle branch block beat
    - E: Ventricular escape beat
    
    Args:
        record_name: MIT-BIH record identifier (e.g., '100')
        
    Returns:
        List of dictionaries, each containing:
            - sample_index: Sample position in the signal
            - symbol: Annotation type (e.g., 'N', 'V', 'A')
            - timestamp: Time in seconds (calculated from sample_index / fs)
            
    Raises:
        FileNotFoundError: If annotation file cannot be found
        ValueError: If annotation format is invalid
    """
    try:
        logger.info(f"Parsing annotations for record {record_name}...")
        
        # Read annotation file (.atr file)
        # rdann downloads the annotation file from PhysioNet if not cached
        annotation = wfdb.rdann(record_name, 'atr', pn_dir='mitdb')
        
        # Extract annotation data
        sample_indices = annotation.sample
        symbols = annotation.symbol
        
        # Build annotation list with calculated timestamps
        # Note: We'll need the sampling rate to calculate timestamps,
        # but for now we'll store sample indices and calculate timestamps later
        annotations = []
        for idx, symbol in zip(sample_indices, symbols):
            annotations.append({
                'sample_index': int(idx),
                'symbol': symbol
            })
        
        logger.info(f"Parsed {len(annotations)} annotations.")
        
        return annotations
        
    except Exception as e:
        if 'FileNotFound' in str(type(e).__name__) or 'not found' in str(e).lower():
            logger.error(f"Annotation file for record {record_name} not found.")
            raise FileNotFoundError(f"Annotation file not found: {e}")
        else:
            logger.error(f"Error parsing annotations: {e}")
            raise ValueError(f"Invalid annotation format: {e}")


def print_summary(record_name: str, signals: np.ndarray, fs: int, annotations: List[Dict[str, Any]]) -> None:
    """
    Print a comprehensive summary of the ECG data.
    
    This function displays:
    - Record metadata (ID, sampling rate, duration)
    - Sample values preview
    - Annotation statistics
    - Detected abnormal beat types
    
    Args:
        record_name: MIT-BIH record identifier
        signals: ECG signal array (shape: num_samples x num_channels)
        fs: Sampling rate in Hz
        annotations: List of annotation dictionaries
    """
    # Extract lead1 (first channel) data
    lead1_data = signals[:, 0]
    num_samples = len(lead1_data)
    duration_seconds = num_samples / fs
    duration_minutes = duration_seconds / 60
    
    # Calculate timestamps for annotations
    for ann in annotations:
        ann['timestamp'] = ann['sample_index'] / fs
    
    # Extract unique abnormal types (excluding 'N' for normal beats)
    abnormal_types = sorted(set(ann['symbol'] for ann in annotations if ann['symbol'] != 'N'))
    
    # Annotation type descriptions
    type_descriptions = {
        'N': 'Normal',
        'V': 'PVC',
        'A': 'Atrial Premature',
        'L': 'Left bundle branch block',
        'R': 'Right bundle branch block',
        'E': 'Ventricular escape',
        'F': 'Fusion',
        'J': 'Nodal escape',
        'Q': 'Unclassifiable',
        '/': 'Paced',
        'S': 'Supraventricular premature',
        'a': 'Aberrated atrial premature',
        'j': 'Nodal premature',
        'e': 'Atrial escape',
        'f': 'Fusion of paced and normal'
    }
    
    # Print summary
    print("\n" + "=" * 50)
    print("=== ECG Data Summary ===")
    print("=" * 50)
    print(f"Record ID: {record_name}")
    print(f"Sampling Rate: {fs} Hz")
    print(f"Total Samples: {num_samples:,}")
    print(f"Duration: {duration_seconds:.2f} seconds ({duration_minutes:.2f} minutes)")
    
    # Print first 20 samples
    print("\n" + "=" * 50)
    print("=== First 20 Samples ===")
    print("=" * 50)
    for i in range(min(20, num_samples)):
        print(f"Sample {i}: {lead1_data[i]:.3f}")
    
    # Print annotation summary
    print("\n" + "=" * 50)
    print("=== Annotation Summary ===")
    print("=" * 50)
    print(f"Total Annotations: {len(annotations)}")
    if abnormal_types:
        print(f"Detected Abnormal Types: {abnormal_types}")
    else:
        print("Detected Abnormal Types: []")
    
    # Print first 5 annotations
    print("\n" + "=" * 50)
    print("=== First 5 Annotations ===")
    print("=" * 50)
    for i, ann in enumerate(annotations[:5]):
        symbol = ann['symbol']
        desc = type_descriptions.get(symbol, 'Unknown')
        print(f"[{i}] Sample: {ann['sample_index']}, "
              f"Type: {symbol} ({desc}), "
              f"Time: {ann['timestamp']:.3f}s")


def main():
    """
    Main function to execute the MIT-BIH data loading and parsing workflow.
    
    This function:
    1. Loads MIT-BIH record 100
    2. Parses its annotation file
    3. Displays a comprehensive summary
    """
    record_name = '100'
    
    try:
        # Load ECG signal data
        signals, fs = load_record(record_name)
        
        # Parse annotations
        annotations = parse_annotations(record_name)
        
        # Print summary
        print_summary(record_name, signals, fs, annotations)
        
        logger.info("Data loading and parsing completed successfully.")
        
    except ConnectionError as e:
        logger.error("Connection error occurred. Please check your internet connection.")
        sys.exit(1)
    except FileNotFoundError as e:
        logger.error("File not found error occurred.")
        sys.exit(1)
    except ValueError as e:
        logger.error("Data format error occurred.")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        sys.exit(1)


if __name__ == '__main__':
    main()

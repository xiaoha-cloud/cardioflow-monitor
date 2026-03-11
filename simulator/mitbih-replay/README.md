# MIT-BIH Replay Simulator

A Python tool for reading and replaying ECG data from the MIT-BIH Arrhythmia Database.

## Overview

This simulator reads ECG records from the MIT-BIH Arrhythmia Database using the `wfdb` library and provides functionality to:
- Load ECG signal data and sampling rates
- Parse annotation files for arrhythmia detection
- Display data summaries and statistics
- Replay ECG data to Kafka topics in real-time (or at specified speed)

## Prerequisites

- Python 3.8 or higher
- Internet connection (for downloading MIT-BIH records from PhysioNet on first run)
- Kafka (for replay functionality) - see project root `scripts/kafka/` for local setup

## Setup

1. **Create a virtual environment:**
   ```bash
   python3 -m venv venv
   ```

2. **Activate the virtual environment:**
   - macOS/Linux: `source venv/bin/activate`
   - Windows: `venv\Scripts\activate`

3. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

## Usage

### Loading and Displaying Data

Run the main script to load and display MIT-BIH record 100:

```bash
python main.py
```

On first run, the script will automatically download the record from PhysioNet.

### Replay to Kafka

After loading data locally, you can replay the ECG data to Kafka:

1. **Ensure Kafka is running:**
   ```bash
   # From project root
   scripts/kafka/ensure-topics.sh
   ```

2. **Set environment variable (optional):**
   ```bash
   export KAFKA_BOOTSTRAP_SERVERS=localhost:9092
   ```
   
   Or create a `.env` file in the `simulator/mitbih-replay/` directory:
   ```
   KAFKA_BOOTSTRAP_SERVERS=localhost:9092
   ```

3. **Run replay:**
   ```bash
   # Replay all samples (real-time speed)
   python replay.py --record 100
   
   # Replay first 1000 samples (for testing)
   python replay.py --record 100 --limit 1000
   
   # Replay at 2x speed
   python replay.py --record 100 --speed 2.0
   
   # Use custom Kafka server
   python replay.py --record 100 --bootstrap-servers localhost:9093
   ```

4. **Verify messages in Kafka:**
   ```bash
   # In another terminal
   docker compose -f scripts/kafka/docker-compose.yml exec kafka \
     kafka-console-consumer --bootstrap-server localhost:9092 \
     --topic ecg.telemetry --from-beginning
   ```

## Output

The script displays:
- Record metadata (ID, sampling rate, duration)
- First 20 sample values
- Annotation summary (total count, detected abnormal types)
- First 5 annotations with details

## MIT-BIH Database

The MIT-BIH Arrhythmia Database contains 48 half-hour excerpts of two-channel ambulatory ECG recordings. Each record includes:
- ECG signal data (sampled at 360 Hz)
- Annotation files with beat-by-beat annotations
- Various arrhythmia types (Normal, PVC, Atrial Premature, etc.)

## Annotation Types

Common annotation symbols:
- **N**: Normal beat
- **V**: Premature Ventricular Contraction (PVC)
- **A**: Atrial Premature beat
- **L**: Left bundle branch block beat
- **R**: Right bundle branch block beat
- **E**: Ventricular escape beat

For more information, visit: https://physionet.org/content/mitdb/1.0.0/

#!/usr/bin/env python3
"""
StorageSharp Benchmark Chart Generator
CSVファイルからグラフ画像（PNG）を生成します
"""

import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import os
import sys
from pathlib import Path
import glob

def setup_output_directory():
    """出力ディレクトリを作成"""
    output_dir = Path("BenchmarkResults")
    output_dir.mkdir(exist_ok=True)
    return output_dir

def load_benchmark_data():
    """ベンチマークデータを読み込み"""
    # BenchmarkDotNetの出力CSVファイルを検索（新しい出力ディレクトリに対応）
    csv_patterns = [
        "BenchmarkResults/results/*-report.csv",
        "BenchmarkDotNet.Artifacts/results/*-report.csv"  # フォールバック
    ]
    
    csv_files = []
    for pattern in csv_patterns:
        csv_files.extend(glob.glob(pattern))
    
    if not csv_files:
        print(f"Error: No benchmark CSV files found matching patterns: {csv_patterns}")
        sys.exit(1)
    
    # 最新のCSVファイルを使用
    csv_path = max(csv_files, key=os.path.getctime)
    print(f"Loading benchmark data from: {csv_path}")
    
    try:
        df = pd.read_csv(csv_path)
        print(f"Raw CSV loaded: {len(df)} rows, columns: {list(df.columns)}")
        
        # NA値を適切に処理
        df = df.replace('NA', np.nan)
        
        # BenchmarkDotNetの時間文字列を数値に変換する関数
        def parse_time_string(time_str):
            if pd.isna(time_str) or time_str == 'NA':
                return np.nan
            
            try:
                # カンマを削除
                time_str = str(time_str).replace(',', '')
                
                # 単位を判定して数値に変換
                if 'ns' in time_str:
                    return float(time_str.replace(' ns', ''))
                elif 'μs' in time_str:
                    return float(time_str.replace(' μs', '')) * 1000
                elif 'ms' in time_str:
                    return float(time_str.replace(' ms', '')) * 1000000
                elif 's' in time_str:
                    return float(time_str.replace(' s', '')) * 1000000000
                else:
                    # 単位がない場合は数値として解析
                    return float(time_str)
            except:
                return np.nan
        
        # メモリ使用量を数値に変換する関数
        def parse_memory_string(memory_str):
            if pd.isna(memory_str) or memory_str == 'NA':
                return np.nan
            
            try:
                memory_str = str(memory_str).replace(',', '')
                
                if 'B' in memory_str:
                    return float(memory_str.replace(' B', ''))
                elif 'KB' in memory_str:
                    return float(memory_str.replace(' KB', '')) * 1024
                elif 'MB' in memory_str:
                    return float(memory_str.replace(' MB', '')) * 1024 * 1024
                else:
                    return float(memory_str)
            except:
                return np.nan
        
        # 時間関連の列を変換
        time_columns = ['Mean', 'Error', 'StdDev']
        for col in time_columns:
            if col in df.columns:
                df[col] = df[col].apply(parse_time_string)
        
        # メモリ関連の列を変換
        memory_columns = ['Allocated']
        for col in memory_columns:
            if col in df.columns:
                df[col] = df[col].apply(parse_memory_string)
        
        # GC関連の列を数値に変換
        gc_columns = ['Gen0', 'Gen1', 'Gen2']
        for col in gc_columns:
            if col in df.columns:
                df[col] = pd.to_numeric(df[col], errors='coerce')
        
        print(f"After parsing - Mean column sample: {df['Mean'].head()}")
        
        # 有効なデータのみをフィルタリング（Meanが数値の行のみ）
        df_valid = df.dropna(subset=['Mean'])
        
        print(f"Loaded benchmark data: {len(df)} total methods, {len(df_valid)} valid methods")
        print(f"Valid methods: {list(df_valid['Method'])}")
        
        if len(df_valid) == 0:
            print("Warning: No valid benchmark data found!")
            return df  # 空のデータフレームを返す
        
        return df_valid
        
    except Exception as e:
        print(f"Error loading CSV: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)

def generate_simple_charts(df, output_dir):
    """シンプルなグラフを生成"""
    if len(df) == 0:
        print("No valid data for charts")
        return
    
    # 応答時間のグラフ
    plt.figure(figsize=(12, 6))
    methods = df['Method']
    means = df['Mean']
    
    plt.bar(range(len(methods)), means)
    plt.xlabel('Methods')
    plt.ylabel('Mean Time (ns)')
    plt.title('Benchmark Results')
    plt.xticks(range(len(methods)), methods, rotation=45, ha='right')
    plt.tight_layout()
    
    output_path = output_dir / "simple_benchmark.png"
    plt.savefig(output_path, dpi=300, bbox_inches='tight')
    print(f"Saved: {output_path}")
    plt.close()

def generate_csv_report(df, output_dir):
    """CSVレポートを生成"""
    if len(df) == 0:
        print("No valid data for CSV report")
        return
    
    # 基本的な列のみを選択
    report_df = df[['Method', 'Mean', 'Error', 'StdDev', 'Allocated']].copy()
    
    # CSVファイルに保存
    csv_path = output_dir / "Benchmark_Comparison.csv"
    report_df.to_csv(csv_path, index=False)
    print(f"Saved CSV report: {csv_path}")
    
    # テキストサマリーも生成
    summary_text = f"""StorageSharp Benchmark Summary
Generated: {pd.Timestamp.now().strftime('%Y-%m-%d %H:%M:%S')}

Total Methods: {len(df)}
Valid Methods: {len(df.dropna(subset=['Mean']))}

Top 5 Fastest Methods (by Mean Time):
"""
    
    # 平均時間でソート
    df_sorted = df.sort_values('Mean', ascending=True)
    for i, (_, row) in enumerate(df_sorted.head().iterrows()):
        method = row['Method']
        mean_time = row['Mean']
        if pd.notna(mean_time):
            if mean_time >= 1_000_000:
                time_str = f"{mean_time/1_000_000:.2f} ms"
            elif mean_time >= 1_000:
                time_str = f"{mean_time/1_000:.2f} μs"
            else:
                time_str = f"{mean_time:.2f} ns"
            summary_text += f"{i+1}. {method}: {time_str}\n"
    
    # ファイルに保存
    summary_path = output_dir / "Benchmark_Summary.txt"
    with open(summary_path, 'w', encoding='utf-8') as f:
        f.write(summary_text)
    
    print(f"Generated summary report: {summary_path}")

def main():
    """メイン処理"""
    print("StorageSharp Benchmark Chart Generator")
    print("=" * 50)
    
    # 出力ディレクトリを作成
    output_dir = setup_output_directory()
    
    # データを読み込み
    df = load_benchmark_data()
    
    # シンプルなグラフを生成
    print("\nGenerating simple charts...")
    generate_simple_charts(df, output_dir)
    
    # CSVレポートを生成
    print("\nGenerating CSV report...")
    generate_csv_report(df, output_dir)
    
    print("\nChart generation completed!")
    print(f"Output directory: {output_dir.absolute()}")

if __name__ == "__main__":
    main() 
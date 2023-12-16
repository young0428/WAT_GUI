using MathNet.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WAT
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WAT());
        }
    }
    public class SignalProcess
    {
        public double[] signal;
        public double[] diff_signal_1;
        public double[] second_diff_signal;
        public double upper_threshold;
        public double lower_threshold;
        public double upper_baseline_threshold;
        public double lower_baseline_threshold;
        public double second_upper_threshold;
        public double baseline_ratio = 0.2;
        public double threshold_ratio = 0.6;
        public double second_threshold_ratio = 0.1;
        public int sampling_rate = 50;
        public double initial_value;
        public double blink_detect_period = 0.15;


        public SignalProcess(double[] input)
        {
            signal = new double[input.Length];
            Array.Copy(input, signal, input.Length);
        }

        public double CalculateGazePosition(double currentEog, double topEog, double bottomEog)
        {
            // 현재 EOG 값이 화면의 어디에 위치하는지 계산
            double gazePosition = (currentEog - topEog) / (bottomEog - topEog);

            // 결과는 0과 1 사이의 값으로, 0에 가까울수록 화면 상단, 1에 가까울수록 화면 하단에 시선이 위치함

            return gazePosition;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        // 1차 미분 계산
        public double[] Differential(double[] signal)
        {
            int length = signal.Length;
            double[] diff_signal_1 = new double[length - 1];

            for (int i = 1; i < length; i++)
            {
                diff_signal_1[i - 1] = signal[i] - signal[i - 1];
            }

            return diff_signal_1;
        }

        // 2차 미분 계산
        public double[] SecondDiff()
        {
            int length = signal.Length;
            second_diff_signal = new double[length - 2];

            for (int i = 1; i < length - 1; i++)
            {
                second_diff_signal[i - 1] = signal[i + 1] - 2 * signal[i] + signal[i - 1];
            }

            return second_diff_signal;
        }

        public (double, double) PeakMinMax(double[] signal, double[] peak_data, int lag)
        {
            int state = 0;
            int inf = 99999999;
            double max_peak_value = 0;
            double min_peak_value = inf;
            int max_detect = 0;
            int max_count = 0;

            int min_detect = 0;
            int min_count = 0;
            double max_sum = 0;
            double min_sum = 0;
            int current_state = 0;
            for (int i = lag; i < signal.Length; i++)
            {
                if(current_state != peak_data[i] || i+1 == signal.Length)
                {
                    if(current_state == 1)
                    {
                        max_count++;
                        max_sum += max_peak_value;
                        max_peak_value = 0;
                    }
                    else if(current_state == -1)
                    {
                        min_count++;
                        min_sum += min_peak_value;
                        min_peak_value = inf;
                    }
                }
                if (peak_data[i] == 1)
                {
                    
                    if (max_peak_value < signal[i])
                    {
                        max_peak_value = signal[i];   // Diff, second_diff 데이터는 index가 한칸씩 당겨져있음
                    }
                }
                else if (peak_data[i] == -1)
                {
                    
                    if (min_peak_value > peak_data[i])
                    {
                        min_peak_value = signal[i];
                    }
                }
                current_state = (int)peak_data[i];
            }
            max_peak_value = max_sum / max_count;
            min_peak_value = min_sum / min_count;
            return (max_peak_value, min_peak_value);
        }
        public int[] BlinkDetection(double[] filtered_first_diff_signal, double max_value, double min_value)
        {
            int len = filtered_first_diff_signal.Length;
            int[] blink_detect_result = new int[len];
            double upper_baseline_threshold = this.baseline_ratio * max_value;
            double lower_baseline_threshold = this.baseline_ratio * min_value;
            double upper_threshold = this.threshold_ratio * max_value;
            double second_upper_threshold = this.second_threshold_ratio * max_value;
            double lower_threshold = this.threshold_ratio * min_value;
            double blink_detect_period = 0.2; // second
            double blink_period_max = 0;
            double second_sum = 0;
            double inf = 9999999;
            double blink_period_min = inf;

            int blink_start_index, blink_end_index;
            int cross_upper_threshold, cross_lower_threshold, cross_second_upper_threshold;
            cross_upper_threshold = 0;
            cross_lower_threshold = 0;
            cross_second_upper_threshold = 0;

            this.upper_threshold = upper_threshold;
            this.lower_threshold = lower_threshold;
            this.upper_baseline_threshold = upper_baseline_threshold;
            this.lower_baseline_threshold = lower_baseline_threshold;
            this.second_upper_threshold = second_upper_threshold;
            this.blink_detect_period = blink_detect_period;


            int cross_second_upeer_idx = 0;
            int blink_detected = 0;
            blink_start_index = -1;
            blink_end_index = -1;

            cross_second_upper_threshold = 0;
            cross_upper_threshold = 0;
            cross_lower_threshold = 0;
            int blink_type = 0;     // 1 == unintended, 2 = intended
            int blink_end = 0;
   

            int cross_upper_idx = -1;
            int cross_lower_idx = -1;
            int detect_time_limit = (int)(this.sampling_rate * this.blink_detect_period * 1);
            int detect_timer = 99999999;


            for (int i = 0; i < len; i++)
            {

                if (blink_start_index == -1 && filtered_first_diff_signal[i] >= upper_baseline_threshold)
                {
                    blink_start_index = i;
                    //Console.Write("upper base");
                    //Console.WriteLine(i);
                    detect_timer = detect_time_limit;

                    continue;
                }
                if (blink_start_index != -1 && cross_upper_threshold == 0 && filtered_first_diff_signal[i] < upper_baseline_threshold)
                {

                    cross_upper_threshold = 0;
                    cross_lower_threshold = 0;
                    cross_second_upper_threshold = 0;
                    cross_second_upeer_idx = 0;
                    cross_second_upper_threshold = 0;
                    cross_upper_threshold = 0;
                    cross_lower_threshold = 0;
                    blink_detected = 0;
                    blink_start_index = -1;
                    blink_period_min = inf;
                    blink_end_index = -1;
                    blink_type = 0;     // 1 == unintended, 2 = intended
                    cross_upper_idx = -1;
                    blink_period_max = 0;
                    cross_lower_idx = -1;
                    detect_timer = 99999999;
                    //Console.Write("base return");

                    //Console.WriteLine(i);
                    continue;
                }
                if (blink_start_index != -1 && cross_upper_threshold == 0 && filtered_first_diff_signal[i] >= upper_threshold)
                {
                    Console.Write("cross upper threshold");
                    Console.WriteLine(i);
                    if (blink_period_max < filtered_first_diff_signal[i])
                    {
                        blink_period_max = filtered_first_diff_signal[i];
                        second_upper_threshold = blink_period_max * 0.2;
                    }
                    cross_upper_threshold = 1;
                    cross_upper_idx = i;
                    detect_timer = detect_time_limit;
                    continue;
                }
                if (cross_upper_threshold != 0 && filtered_first_diff_signal[i] <= lower_threshold)
                {
                    Console.Write("cross lower threshold after upper : ");
                    Console.WriteLine(i);
                    if (blink_period_min > filtered_first_diff_signal[i])
                    {
                        blink_period_min = filtered_first_diff_signal[i];
                    }
                    
                    cross_lower_idx = i;
                    cross_lower_threshold = 1;
                    detect_timer = detect_time_limit;
                    cross_second_upper_threshold = 0;
                    continue;
                }
                //if (cross_lower_threshold != 0 && filtered_first_diff_signal[i] >= lower_baseline_threshold && blink_end_index == -1)
                //{
                //    cross_lower_threshold = 0;
                //    detect_timer = 99999999;
                //    continue;
                //}
                // 여기까지 blink detection
                // 이후는 intended blink detection
                if (cross_lower_threshold==1 && second_upper_threshold < filtered_first_diff_signal[i] && cross_second_upper_threshold != 1)
                {

                    Console.Write("Second upper threshold : ");
                    Console.WriteLine(i);
                    blink_detected = 1;
                    blink_end_index = i;
                    cross_second_upper_threshold = 1;
                    if ((blink_period_max - blink_period_min) > (upper_threshold - lower_threshold) * 2.1)
                    {
                        blink_type = 2;
                    }
                    else blink_type = 1;
                    detect_timer = (int)(detect_time_limit);
                    continue;
                }
                if (blink_end_index == -1
                    && cross_second_upper_threshold != 1
                    && (i - cross_lower_idx) >= detect_time_limit*2
                    && blink_type == 0)
                {
                    Console.Write("timeover : ");
                    Console.WriteLine(i);
                    blink_type = 1;
                }
                if (blink_end_index != -1 && cross_second_upper_threshold == 1 && second_upper_threshold < filtered_first_diff_signal[i])
                { 
                    blink_end_index = i;
                    
                    //detect_timer = 99999;
                }

                if (blink_detected == 1 && (blink_type != 0 || i + 1 >= len))
                {
                    Console.Write("set : ");
                    Console.WriteLine(i);

                    Console.Write("start_idx : ");
                    Console.WriteLine(blink_start_index);
                    Console.Write("end idx : ");
                    Console.WriteLine(blink_end_index);


                    Console.Write("blink_type : ");
                    Console.WriteLine(blink_type);
                    if (blink_type == 0) blink_type = 1;
                    for (int j = blink_start_index; j <= blink_end_index ; j++)
                    {
                        blink_detect_result[j] = blink_type;
                    }
                    cross_upper_threshold = 0;
                    cross_lower_threshold = 0;
                    cross_second_upper_threshold = 0;
                    cross_second_upeer_idx = 0;
                    blink_period_max = 0;
                    blink_period_min = inf;
                    blink_detected = 0;
                    blink_start_index = -1;
                    blink_end_index = -1;
                    cross_second_upper_threshold = 0;
                    cross_upper_threshold = 0;
                    cross_lower_threshold = 0;
                    blink_type = 0;     // 1 == unintended, 2 = intended
                    cross_upper_idx = -1;
                    cross_lower_idx = -1;
                    detect_timer = 9999999;
                }
                detect_timer--;
                if (detect_timer < 0)
                {
                    cross_upper_threshold = 0;
                    cross_lower_threshold = 0;
                    cross_second_upper_threshold = 0;
                    cross_second_upeer_idx = 0;
                    cross_second_upper_threshold = 0;
                    cross_upper_threshold = 0;
                    cross_lower_threshold = 0;
                    blink_detected = 0;
                    blink_start_index = -1;
                    blink_period_max = 0;
                    blink_period_min = inf;
                    blink_end_index = -1;
                    blink_type = 0;     // 1 == unintended, 2 = intended
                    cross_upper_idx = -1;
                    cross_lower_idx = -1;
                    //Console.WriteLine("Timeout");
                }
            }
            return blink_detect_result;

        }
        public double[] IntegrateSignal(double initialValue, double[] derivatives)
        {
            int length = derivatives.Length;

            // 초기 값은 그대로 사용
            double[] originalSignal = new double[length + 1];
            originalSignal[0] = initialValue;

            // 1차 미분 값으로부터 원래의 신호를 역산하여 누적합 계산
            for (int i = 1; i <= length; i++)
            {
                originalSignal[i] = originalSignal[i - 1] + derivatives[i - 1];
            }

            return originalSignal;
        }
        public (double[], double[]) FindPeaks(double[] data, int lag, double threshold, double influence)
        {
            int n = data.Length;
            double[] signals = new double[n];
            double[] filteredY = new double[n];
            double[] avgFilter = new double[n];
            double[] stdFilter = new double[n];

            // Initialize variables
            for (int i = 0; i < lag; i++)
            {
                signals[i] = 0;
                filteredY[i] = data[i];
            }

            avgFilter[lag - 1] = Mean(data, 0, lag);
            stdFilter[lag - 1] = StandardDeviation(data, 0, lag);

            // Main loop
            for (int i = lag; i < n; i++)
            {
                if (Math.Abs(data[i] - avgFilter[i - 1]) > threshold * stdFilter[i - 1])
                {
                    if (data[i] > avgFilter[i - 1])
                    {
                        signals[i] = 1; // Positive signal
                                        // Positive Peak
                    }
                    else
                    {
                        signals[i] = -1; // Negative signal
                                         // Negative Peak
                    }

                    filteredY[i] = influence * data[i] + (1 - influence) * filteredY[i - 1];
                }
                else
                {
                    signals[i] = 0; // No signal
                    filteredY[i] = data[i];
                }

                avgFilter[i] = Mean(filteredY, i - lag + 1, lag);
                stdFilter[i] = StandardDeviation(filteredY, i - lag + 1, lag);
            }

            return (signals, filteredY);
        }

        static double Mean(double[] data, int start, int length)
        {
            double sum = 0;
            for (int i = start; i < start + length; i++)
            {
                sum += data[i];
            }
            return sum / length;
        }


        static double StandardDeviation(double[] data, int start, int length)
        {
            double mean = Mean(data, start, length);
            double sumSquaredDifferences = 0;

            for (int i = start; i < start + length; i++)
            {
                sumSquaredDifferences += Math.Pow(data[i] - mean, 2);
            }

            return Math.Sqrt(sumSquaredDifferences / length);
        }

        public double[] ButterworthHighpassLowpassFilter(double[] signal, double samplingRate, double highpassCutoff, double lowpassCutoff, int order)
        {
            // 하이패스 필터 생성
            var highpassFilter = OnlineFilter.CreateHighpass(ImpulseResponse.Finite, samplingRate, highpassCutoff, order);

            // 로우패스 필터 생성
            var lowpassFilter = OnlineFilter.CreateLowpass(ImpulseResponse.Finite, samplingRate, lowpassCutoff, order);

            // 입력 신호를 하이패스 필터링
            var highpassFilteredSignal = new double[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                highpassFilteredSignal[i] = highpassFilter.ProcessSample(signal[i]);
            }

            // 하이패스 필터링된 결과를 로우패스 필터링
            var lowpassFilteredSignal = new double[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                lowpassFilteredSignal[i] = lowpassFilter.ProcessSample(highpassFilteredSignal[i]);
            }

            return lowpassFilteredSignal;
        }

        public double[] RemovePeakAndInterpolate(double[] signal, int[] peakArray)
        {
            if (signal.Length != peakArray.Length)
            {
                throw new ArgumentException("Signal and peakArray must have the same length.");
            }

            // 각 Peak의 시작과 끝을 저장할 배열
            int[] startIndices = new int[peakArray.Length];
            int[] endIndices = new int[peakArray.Length];
            int peakCount = 0;

            // Peak의 시작과 끝을 찾아내기
            for (int i = 0; i < peakArray.Length; i++)
            {
                if (peakArray[i] != 0)
                {
                    startIndices[peakCount] = i;
                    endIndices[peakCount] = i;

                    // Peak의 시작 찾기
                    while (startIndices[peakCount] > 0 && peakArray[startIndices[peakCount] - 1] != 0)
                    {
                        startIndices[peakCount]--;
                    }

                    // Peak의 끝 찾기
                    while (endIndices[peakCount] < peakArray.Length - 1 && peakArray[endIndices[peakCount] + 1] != 0)
                    {
                        endIndices[peakCount]++;
                    }

                    peakCount++;
                }
            }
            // 각 Peak에 대해 처리
            for (int peakIndex = 0; peakIndex < peakCount; peakIndex++)
            {
                int startIdx = startIndices[peakIndex];
                int endIdx = endIndices[peakIndex];

                // 시작과 끝 지점 사이의 값을 삭제
                for (int i = startIdx; i <= endIdx; i++)
                {
                    signal[i] = 0.0; // 삭제할 값을 0으로 설정
                }
            }

            return signal;
        }

        public int CheckCrossThreshold(double diff_value, double upper_threshold, double lower_threshold)
        {
            // upper를 넘으면 1 , blink 인지 움직임인지 파악
            if (diff_value > upper_threshold) return 1;
            if (diff_value < lower_threshold) return 2;
            return 0;
        }
        public double[] IntegrateSignalWithPeak(double[] originalSignal, double initialValue, double[] derivativeArray, int[] peakArray)
        {
            int length = originalSignal.Length;
            double[] result = new double[length];

            // 초기값 설정
            result[0] = initialValue;

            // Peak 구간에 대해 1차 미분 배열 적분
            for (int i = 1; i < length; i++)
            {
                if (peakArray[i - 1] != 0)
                {
                    result[i] = result[i - 1] + derivativeArray[i - 1];
                }
                else
                {
                    result[i] = originalSignal[i]; // Peak 구간이 아니면 원래 신호의 값으로 채움
                }
            }

            return result;
        }


    }

    class ScoreCalculator
    {
        public double CalculatePeriodScore(double wink_period)
        {
            double true_min = 0.2;
            double true_max = 0.5;
            if (wink_period < true_min)
            {
                return 20 * (1 - sigmoid(20*(true_min - wink_period))); //  0 ~ 0.5
            }
            if(wink_period > true_max)
            {
                return 20 * (1 - sigmoid(5 * (wink_period - true_max)));
            }
            return 10;
        }
        
        public double CalculateTimingScore(double wink_timing)
        {
            double true_min = 0.3;
            double true_max = 1;

            if (wink_timing < true_min) return 20 * (1 - sigmoid(20 * (true_min - wink_timing))); //  0 ~ 0.5
            if (wink_timing > true_max) return 20 * (1 - sigmoid(5 * (wink_timing - true_max)));
            return 10;
        }
        public double CalculateGazeScore(double face_x, double face_y, double gaze_x, double gaze_y)
        {
            double diff_x = Math.Abs(face_x - gaze_x);
            double diff_y = Math.Abs(face_y - gaze_y);
            double square_size = 30;
            double distance = Math.Sqrt(Math.Pow(diff_x, 2)+Math.Pow(diff_y, 2) );
            return Convert.ToInt16(distance <= square_size)*10 ;

        }


        static double sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }
    }
    class NonlinearRegression
    {
        public void training(double[] eogDeltaValues, double[] trueDeltaValues)
        {
            // 학습 데이터 생성

            // 모델 학습
            TrainModel(eogDeltaValues, trueDeltaValues);
        }



        // 모델 파라미터
        static double weight1 = 0.1;

        // 학습률
        static double learningRate = 0.0001;

        // 비선형 함수 (sin 함수)


        // 모델 학습 함수
        public void TrainModel(double[] eogValues, double[] trueDeltaValues, int epochs = 100)
        {
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                for (int i = 0; i < eogValues.Length; i++)
                {
                    double prediction = predict(eogValues[i]);
                    double error = prediction - trueDeltaValues[i];

                    // 경사 하강법을 사용하여 가중치 업데이트
                    weight1 -= learningRate * error * eogValues[i];
                }
            }
        }

        // 새로운 입력에 대한 예측 함수
        public double predict(double eogValue)
        {
            return weight1 * eogValue;
        }
    }
}

using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Security.AccessControl;

namespace WAT
{
    public partial class WAT : Form
    {
        //Calibration 1
        private int flag = 1;
        private double angle = 0;
        private int x = 0, y = 0;
        private int max_x;
        private int max_y;
        private int X, Y;

        //Calibration 2
        private int cnt = 0;
        private string[] img = new string[6] { @"C:\Users\Miri_73\Desktop\game_1.jpg", @"C:\Users\Miri_73\Desktop\game_2.jpg", @"C:\Users\Miri_73\Desktop\game_3.jpg", @"C:\Users\Miri_73\Desktop\game_4.jpg", @"C:\Users\Miri_73\Desktop\game_5.jpg", @"C:\Users\Miri_73\Desktop\gamebackground.jpg" };

        //Game
        private int img_cnt = 0;

        //Score
        private int score_accuracy = 0;
        private int score_timing = 0;
        private int score_duration = 0;
        private int score_total = 0;
        private string[] comment = new string[4] { "Low Gaze Accuracy", "Wink Timing", "Wink duration", "Wink Genious!" };


        //EOG data
        public int[] data_buff = new int[1200];
        public static int buffsize =5000;
        public double[] data_left_vertical = new double[buffsize];
        public double[] data_right_vertical = new double[buffsize];
        public double[] data_horizontal = new double[buffsize];

        double left_vertical_min, left_vertical_max;
        double[] calibration_data_right_vertical = new double[buffsize];
        double[] calibration_data_horizontal = new double[buffsize];
        double[] saccade_calibration_data_right_vertical = new double[buffsize];
        double[] saccade_calibration_data_horizontal= new double[buffsize];
        int saccade_top = 0;

        static int realtime_data_buffsize = 100;
        int face_on_flag = 0;
        int face_on_counter = 0;
        int face_pos_x = 0;
        int face_pos_y = 0;
        double[] realtime_data_buffer_left_vertical = new double[realtime_data_buffsize];
        double[] realtime_data_buffer_right_vertical = new double[realtime_data_buffsize];
        double[] realtime_data_buffer_horizontal = new double[realtime_data_buffsize];
        double[] realtime_diff_data_buffer_left_vertical = new double[realtime_data_buffsize];
        double[] realtime_diff_data_buffer_right_vertical = new double[realtime_data_buffsize];
        double[] realtime_diff_data_buffer_horizontal = new double[realtime_data_buffsize];

        SignalProcess signal_processor;

        public double[] input_Draw_1 = new double[buffsize];
        public double[] input_Draw_2 = new double[buffsize];
        public double[] input_Draw_3 = new double[buffsize];
        NonlinearRegression vertical_regression, horizontal_regression;
        int lag = 40;
        double threshold = 2.8;
        double influence = 0.08;

        short calibration_start_flag = 0;
        short start_flag = 0;
        int start_byte = 0;
        int data_count = 0;
        int cali_top = 0;
        int Data_1, Data_2, Data_3;
        int sampling_rate = 100;
        int game_start_flag = 0;
        int count = 0;
        double delay = 0.2; // sec
        static int delay_idx = (int)(0.3 * 100);


        
        double vertical_peak_max, vertical_peak_min;
        double vertical_eog_value_on_top = 0;
        double vertical_eog_value_on_bottom = 9999999;
        double horizontal_eog_value_on_left = 0;
        double horizontal_eog_value_on_right = 9999999;
        double horizontal_peak_max, horizontal_peak_min;
        double threshold_ratio = 0.7;
        double second_threshold_ratio = 0.2;
        double baseline_ratio = 0.2;
        double vertical_upper_threshold_value, vertical_lower_threshold_value;
        double horizontal_upper_threshold_value, horizontal_lower_threshold_value;
        double vertical_gaze_pos, horizontal_gaze_pos;

        public struct EOG_position
        {
            public int x, y;
            public double EOG_left_vertical, EOG_right_vertical, EOG_horizontal;
        }

        EOG_position[] Cal_data = new EOG_position[1200];

        //Console
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public WAT()
        {
            InitializeComponent();
            AllocConsole();
            
        }

        private void btnCalibration_Click(object sender, EventArgs e)
        {
            btnReset.Visible = true;
            
            //Calibration 1 
            // 점 따라가기
            if (flag == 2)
            {
                //sPort.Open();
                Tablepanel.Visible = false;
                Cal2text.Visible = false;
                move_black.BackColor = Color.Black;
                move_black.Size = new Size(30, 30);

                timer5.Enabled = true;
                move_black.Visible = true;

                angle = 0;
                timer1.Enabled = true;
                timer2.Enabled = true;

                timer1.Start();
                timer2.Start();
                calibration_start_flag = 2;
                return;
            }

            //Calicbration 2 
            // blink
            if (flag == 1)
            {
                max_x = move_black.Location.X;
                max_y = move_black.Location.Y;
                //sPort.Open();
                timer5.Enabled = true;

                Tablepanel.Visible = false;
                timer3.Enabled = true;
                timer4.Enabled = true;
                Cal2text.Visible = true;
                move_black.Location = new Point(max_x/2, max_y/2);
                move_black.Visible = true;
                move_black.Size = new Size(50, 50);
                calibration_start_flag = 1;
                return;
            }

            // game Start
            if (flag == 3)
            {
                //sPort.Open();
                Game_Panel.Visible = true;
                game_timer.Enabled = true;
                img_cnt = 0;

                timer5.Enabled = true;
                Tablepanel.Visible=false;
                game_start_flag = 1;
                return;
            }

            // End
            if (flag == 4)
            {

            }
        }
        private void ClearAndClosePort()
        {
            if(sPort.IsOpen)
            {
                string _ = sPort.ReadExisting();
                sPort.Close();
            }
        }
        //Reset 수정
        private void btnReset_Click(object sender, EventArgs e)
        {
             //Calibration1
            if (flag == 2)
            {
                ClearAndClosePort();
                timer5.Enabled = false;

                Tablepanel.Visible = false;
                timer1.Enabled = false;
                timer2.Enabled = false;
                move_black.Visible = false;

                move_black.Location = new Point(max_x, max_y);
                return;
            }

            //Calibration2
            if (flag == 1)
            {
                ClearAndClosePort();
                timer5.Enabled = false;

                Tablepanel.Visible = false;
                timer3.Enabled = false;
                timer4.Enabled = false;
                move_black.BackColor = Color.Black;
                return;
            }
            if (flag == 3)
            {
                Game_Image.ImageLocation = @"C:\Users\Miri_73\Desktop\game_gamebackground.jpg";
                img_cnt = 0;
                game_timer.Enabled = false;
                return;
            }
        }

        private void change_img(object sender, EventArgs e)
        {
            if (img_cnt == 0)
            {
                Game_Image.ImageLocation = img[5];
            }
            else if ((img_cnt < 11)&&(img_cnt%2 == 1))
            {
                Game_Image.ImageLocation = img[(img_cnt-1)/2];
            }
            else if (img_cnt >10)
            {
                Game_Image.ImageLocation = img[5];
                btnNext.Visible = true;
                game_timer.Enabled = false;
            }
            else
            {
                Game_Image.ImageLocation = img[5];
            }
            img_cnt++;
        }

        private void UpdateLocation()
        {
            x = (int)(max_x / 2 * Math.Cos(angle));
            y = (int)(max_y / 2 * Math.Sin(angle));

            X = max_x / 2 + x;
            Y = max_y / 2 + y;

            move_black.Location = new Point(X , Y);
           
            angle += 0.02 * Math.PI;

        }
        private void move_timer(object sender, EventArgs e)
        {
            UpdateLocation();
        }

        private void timer1_stop(object sender, EventArgs e)
        {
            // 돌아가는거 끝나고 calibraiton
            timer5.Enabled = false;
            timer5.Stop();

            timer1.Enabled = false;
            timer1.Stop();
            btnNext.Visible = true;
            timer2.Enabled = false;
            timer2.Stop();
            ClearAndClosePort();
            calibration_start_flag = 0;
            saccade_calibration();

            //signal_processor = new SignalProcess(calibration_data_right_vertical);



            //double[] blink_removed_diff_signal = signal_processor.RemovePeakAndInterpolate(vertical_diff_result, blink_detection_result);



        }
        
        private void blink_calibration()
        {
            double[] vertical_diff_result = signal_processor.Differential(calibration_data_right_vertical);
            double[] filtered_vertical_diff_signal = signal_processor.ButterworthHighpassLowpassFilter(vertical_diff_result, sampling_rate, 0.1, 1000, 2);
            double[] peak_detection_result, filtered_y;
            (peak_detection_result, filtered_y) = signal_processor.FindPeaks(filtered_vertical_diff_signal, lag, threshold, influence);
            (vertical_peak_max, vertical_peak_min) = signal_processor.PeakMinMax(filtered_vertical_diff_signal, peak_detection_result, lag);
            vertical_upper_threshold_value = baseline_ratio * vertical_peak_max;
            vertical_lower_threshold_value = baseline_ratio * vertical_peak_min;
            int[] blink_detection_result = signal_processor.BlinkDetection(filtered_vertical_diff_signal, vertical_peak_max, vertical_peak_min);
            double[] test_value = new double[1000];
            for (int i = 0; i < 1000; i++) test_value[i] = 1000;

            //scope2.Channels[0].Data.SetYData(test_value);
            scope2.Channels[0].Data.SetYData(filtered_vertical_diff_signal);
        }
        private void saccade_calibration()
        {
            double[] vertical_diff_result = signal_processor.Differential(saccade_calibration_data_right_vertical);
            double[] filtered_vertical_diff_signal = signal_processor.ButterworthHighpassLowpassFilter(vertical_diff_result, sampling_rate, 0.1, 1000, 2);
            int[] blink_detection_result = signal_processor.BlinkDetection(filtered_vertical_diff_signal, vertical_peak_max, vertical_peak_min);
            double[] blink_removed_diff_signal = signal_processor.RemovePeakAndInterpolate(vertical_diff_result, blink_detection_result);
            double[] vertical_eog_diff_for_regression = new double[1000];
            double[] vertical_position_diff_for_regression = new double[1000];
            int vertical_regression_top = 0;
            for(int i=0;i<blink_removed_diff_signal.Length;i++)
            {
                if (Convert.ToBoolean(signal_processor.CheckCrossThreshold(blink_removed_diff_signal[i], vertical_upper_threshold_value, vertical_lower_threshold_value)))
                {
                    //vertical_eog_diff_for_regression[vertical_regression_top] = filtered_vertical_diff_signal[i];
                   // vertical_position_diff_for_regression[vertical_regression_top] = Cal_data[i + 1].y - Cal_data[i].y;
                    //vertical_regression_top++;

                }
            }


            double[] horizontal_diff_result = signal_processor.Differential(saccade_calibration_data_horizontal);
            double[] horizontal_filtered = signal_processor.ButterworthHighpassLowpassFilter(horizontal_diff_result, sampling_rate, 0.1, 1000, 2);
            double[] blink_removed_horizontal_diff_signal = signal_processor.RemovePeakAndInterpolate(horizontal_filtered, blink_detection_result);
            double[] horizontal_peak_detection_result, horizontal_filtered_y;
            (horizontal_peak_detection_result, horizontal_filtered_y)= signal_processor.FindPeaks(blink_removed_horizontal_diff_signal, lag, threshold, influence);
            (horizontal_peak_max, horizontal_peak_min) = signal_processor.PeakMinMax(blink_removed_horizontal_diff_signal, horizontal_peak_detection_result, lag);
            horizontal_upper_threshold_value = horizontal_peak_max * 0.5;
            horizontal_lower_threshold_value = horizontal_peak_min * 0.5;
            double[] horizontal_eog_diff_for_regression = new double[1000];
            double[] horizontal_position_diff_for_regression = new double[1000];
            int horizontal_regression_top = 0;

            for (int i = 0; i < blink_removed_horizontal_diff_signal.Length; i++)
            {
                if (Convert.ToBoolean(signal_processor.CheckCrossThreshold(blink_removed_horizontal_diff_signal[i], horizontal_upper_threshold_value, horizontal_lower_threshold_value)))
                {
                    //horizontal_eog_diff_for_regression[horizontal_regression_top] = horizontal_filtered[i];
                    //horizontal_position_diff_for_regression[horizontal_regression_top] = Cal_data[i + 1].x - Cal_data[i].x;
                    //horizontal_regression_top++;
                }
            }
            
            vertical_regression = new NonlinearRegression();
            horizontal_regression = new NonlinearRegression();

            vertical_regression.training(vertical_eog_diff_for_regression, vertical_position_diff_for_regression);
            horizontal_regression.training(horizontal_eog_diff_for_regression, horizontal_position_diff_for_regression);

            double[] vertical_test = new double[5];
            double[] horizontal_test = new double[5];
            double[] vertical_test_pos = new double[5];
            double[] horizontal_test_pos = new double[5];
            vertical_test = vertical_eog_diff_for_regression.Take(5).ToArray();
            horizontal_test = vertical_eog_diff_for_regression.Take(5).ToArray();
            for (int i=0;i<5;i++)
            {
                double predict_pos = vertical_regression.predict(vertical_test[i]);
                Console.WriteLine("//////// vertical ////////");
                Console.Write("Predict : ");
                Console.WriteLine(predict_pos);
                Console.Write("True : ");
                Console.WriteLine(vertical_eog_diff_for_regression[i]);

                predict_pos = horizontal_regression.predict(horizontal_test[i]);
                Console.WriteLine("         horizontal         ");
                Console.Write("Predict : ");
                Console.WriteLine(predict_pos);
                Console.Write("True : ");
                Console.WriteLine(horizontal_eog_diff_for_regression[i]);
            }
            scope2.Channels[0].Data.SetYData(filtered_vertical_diff_signal);
            scope3.Channels[0].Data.SetYData(horizontal_filtered);

        }
        private void btnNext_Click(object sender, EventArgs e)
        {
            btnNext.Visible = false;
            move_black.Visible = false;

            if (flag == 1)
            {
                btnCalibration.Text = "Calibration2";
                Tablepanel.Visible = true;
                return;
            }

            if (flag == 2)
            {
                Cal2text.Visible = false;
                btnReset.Visible = false;
                btnCalibration.Text = "Game Start";
                Tablepanel.Visible = true;
                return;
            }
            
            if (flag == 3)
            {
                btnReset.Visible = false;
                btnCalibration.Text = "Result";
                Game_Panel.Visible = false;
                return;
            }

        }

        //Cal1 -> Cal2 로 btn text 변환될 때
        private void change_flag(object sender, EventArgs e)
        {
            flag += 1;
        }

        private void sPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (sPort.BytesToRead > 0)
            {
                if (sPort.IsOpen)
                {
                    if (start_flag == 0)
                    {
                        start_byte = sPort.ReadByte();
                        count = 0;
                    }
                }
                if (start_byte == 0x81)
                {
                    start_flag = 1;
                    data_buff[data_count] = sPort.ReadByte();

                    data_count++;

                    if (data_count == 4)
                    {
                        Data_1 = ((data_buff[0] & 0x7f) << 7) + (data_buff[1] & 0x7f);
                        Data_2 = ((data_buff[2] & 0x7f) << 7) + (data_buff[3] & 0x7f);
                        Data_1 -= 7000;
                        Data_2 -= 7000;


                        start_flag = 2;
                        data_count = 0;
                    }


                    if (start_flag == 2)
                    {
                        if(calibration_start_flag == 1)
                        {
                            calibration_data_right_vertical[cali_top] = Data_1;
                            calibration_data_horizontal[cali_top] = Data_2;
                            cali_top++;
                            
                        }

                        if(calibration_start_flag == 2)
                        {
                            saccade_calibration_data_right_vertical[saccade_top] = Data_1;
                            saccade_calibration_data_horizontal[saccade_top] = Data_2;
                            saccade_top++;

                            EOG_position Calibration_Data = new EOG_position
                            {
                                x = X,
                                y = Y,
                                EOG_right_vertical = Data_1,
                                EOG_horizontal = Data_2
                            };

                            Cal_data[count] = Calibration_Data;
                            count++;

                        }
                        if(game_start_flag == 1)
                        {
                            for(int i=0;i<realtime_data_buffsize - 1;i++)
                            {
                                realtime_data_buffer_right_vertical[i] = realtime_data_buffer_right_vertical[i + 1];
                                realtime_data_buffer_horizontal[i] = realtime_data_buffer_horizontal[i + 1];
                                realtime_diff_data_buffer_right_vertical[i] = realtime_diff_data_buffer_right_vertical[i + 1];
                                realtime_diff_data_buffer_horizontal[i] = realtime_diff_data_buffer_horizontal[i + 1];
                            }
                            realtime_data_buffer_right_vertical[realtime_data_buffsize - 1] = Data_1;
                            realtime_data_buffer_horizontal[realtime_data_buffsize - 1] = Data_2;
                            double vertical_diff = Data_1 - realtime_diff_data_buffer_right_vertical[realtime_data_buffsize - 2];
                            double horizontal_diff = Data_2 - realtime_diff_data_buffer_horizontal[realtime_data_buffsize - 2];
                            double delayed_vertical_diff = realtime_diff_data_buffer_right_vertical[realtime_data_buffsize - delay_idx];
                            double delayed_horizontal_diff = realtime_diff_data_buffer_horizontal[realtime_data_buffsize - delay_idx];
                            realtime_diff_data_buffer_right_vertical[realtime_data_buffsize - 1] = vertical_diff;
                            realtime_diff_data_buffer_horizontal[realtime_data_buffsize - 1] = horizontal_diff;

                            if (face_on_flag == 1)
                            {
                                face_on_counter++;
                                if (face_on_counter >= delay_idx + 20)
                                {
                                    vertical_gaze_pos = face_pos_y;
                                    horizontal_gaze_pos = face_pos_x;
                                }
                                if (face_on_counter >= delay_idx + 200)
                                {
                                    face_on_counter = 0;
                                    face_on_flag = 0;
                                }
                            }

                            int slice_start_idx = realtime_data_buffsize - (delay_idx + 2);
                            int slice_end_idx = realtime_data_buffsize - 1;
                            int cross_threshold_flag = signal_processor.CheckCrossThreshold(
                                delayed_vertical_diff, 
                                vertical_upper_threshold_value, 
                                vertical_lower_threshold_value
                                );
                            if(cross_threshold_flag == 1)
                            {
                                
                                double[] sliced_diff_signal = realtime_diff_data_buffer_right_vertical.Skip(realtime_data_buffsize - (delay_idx + 2)).ToArray();
                                int[] blink_detection_result = signal_processor.BlinkDetection(sliced_diff_signal, vertical_peak_max, vertical_peak_min);
                                for(int i=slice_start_idx; i <= slice_end_idx; i++)
                                {
                                    if (blink_detection_result[i-slice_start_idx] != 0)
                                    {
                                        realtime_diff_data_buffer_right_vertical[i] = 0;
                                        realtime_diff_data_buffer_horizontal[i] = 0;
                                    }
                                }
                                int current_state = 0;
                                int wink_start_idx = 0;
                                int wink_end_idx = 0;
                                double wink_period = 0;
                                for (int i = 0; i < blink_detection_result.Length; i++)
                                {
                                    if (blink_detection_result[i] == 2 && current_state == 0)
                                    {
                                        current_state = 2;
                                        wink_start_idx = i;
                                        continue;
                                    }
                                    if (blink_detection_result[i] == 0 && current_state == 2)
                                    {
                                        wink_end_idx = delay_idx + i;
                                        wink_start_idx += delay_idx;
                                        wink_period = (wink_end_idx - wink_start_idx) / sampling_rate;
                                        Console.Write("Wink Period : ");
                                        Console.WriteLine(wink_period);
                                        Console.Write("Gaze Pos vertical : ");
                                        Console.WriteLine(vertical_gaze_pos);
                                        Console.Write("Gaze Pos horizontal : ");
                                        Console.WriteLine(horizontal_gaze_pos);
                                        Console.Write("Face Pos vertical : ");
                                        Console.WriteLine(face_pos_y);
                                        Console.Write("Face Pos horizontal : ");
                                        Console.WriteLine(face_pos_x);


                                        face_on_counter = 0;
                                        face_on_flag = 0;

                                        

                                        vertical_gaze_pos = face_pos_y;
                                        horizontal_gaze_pos = face_pos_x;

                                        current_state = 0;
                                        break;
                                    }
                                }
                                
                            }
                            if (cross_threshold_flag != 0)
                            {
                                double vertictal_delta_pos = vertical_regression.predict(realtime_diff_data_buffer_right_vertical[slice_start_idx + 2]);
                                vertical_gaze_pos += vertictal_delta_pos;
                            }
                            cross_threshold_flag = signal_processor.CheckCrossThreshold(
                                delayed_horizontal_diff,
                                vertical_upper_threshold_value,
                                vertical_lower_threshold_value
                                );
                            if( cross_threshold_flag != 0 )
                            {
                                double horizontal_delta_pos = horizontal_regression.predict(realtime_diff_data_buffer_horizontal[slice_start_idx + 2]);
                                horizontal_gaze_pos += horizontal_delta_pos;
                            }
                            

                        }
                        start_flag = 0;

                    }
                }
                //////////////////////// for Test ////////////////////////

                //if (start_byte == 0x81)
                //{
                //    start_flag = 1;
                //    data_buff[data_count] = sPort.ReadByte();

                //    data_count++;

                //    if (data_count == 4)
                //    {
                //        Data_1 = ((data_buff[0] & 0x7f) << 7) + (data_buff[1] & 0x7f);
                //        Data_2 = ((data_buff[2] & 0x7f) << 7) + (data_buff[3] & 0x7f);

                //        start_flag = 2;
                //        data_count = 0;
                //    }

                //    if (start_flag == 2)
                //    {
                //        Data_1 -= 7000;
                //        Data_2 -= 7000;

                //        for (int i = 0; i < buffsize - 1; i++)
                //        {
                //            data_right_vertical[i] = data_right_vertical[i + 1];
                //            data_horizontal[i] = data_horizontal[i + 1];
                //        }

                //        data_right_vertical[buffsize - 1] = Data_1;
                //        input_Draw_1 = data_right_vertical;

                //        data_horizontal[buffsize - 1] = Data_2;
                //        input_Draw_2 = data_horizontal;

                //        EOG_position Calibration_Data = new EOG_position
                //        {
                //            x = X,
                //            y = Y,
                //            EOG_right_vertical = Data_1,
                //            EOG_horizontal = Data_2
                //        };

                //        Cal_data[count] = Calibration_Data;

                //        count++;

                //        start_flag = 0;
                //    }
                //}

                ///////////////////////////////////////////////////////////////


            }

        }

        private void On_timer(object sender, EventArgs e)
        {
            //scope2.Channels[0].Data.SetYData(data_right_vertical);
            scope3.Channels[0].Data.SetYData(calibration_data_right_vertical);
        }

        private void Calibration2_Timer(object sender, EventArgs e)
        {
            
            int red = Convert.ToInt16(cnt%3 == 0)*255;
            int green = Convert.ToInt16(cnt % 3 == 1) * 255;
            int blue = Convert.ToInt16(cnt % 3 == 2) * 255;

            cnt++;

            move_black.BackColor = Color.FromArgb(red, green, blue);
        }

        private void Calibration2_stop_timer(object sender, EventArgs e)
        {
            //ClearAndClosePort();
            //timer5.Enabled = false;
            //timer5.Stop();

            btnNext.Visible = true;
            move_black.Visible = false;
            timer3.Enabled = false;
            timer4.Enabled = false;
            signal_processor = new SignalProcess(calibration_data_right_vertical);
            //calibration_start_flag = 0;
            
            blink_calibration();
            Console.Write("Cal1 end");

        }
    }
}

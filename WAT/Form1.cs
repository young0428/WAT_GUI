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
        private int[] game_pnt_X = new int[10];
        private int[] game_pnt_Y = new int[10];

        //Score
        private int score_accuracy = 0;
        private int score_timing = 0;
        private int score_duration = 0;
        private int score_total = 0;
        private string[] comment = new string[4] { "Low Gaze Accuracy", "Wink Timing", "Wink duration", "Wink Genious!" };


        //EOG data

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
        static int delay_idx = (int)(0.2 * 50);

        public int[] data_buff = new int[1200];
        public static int buffsize = 12*4*100;
        public double[] data_left_vertical = new double[buffsize];
        public double[] data_right_vertical = new double[buffsize];
        public double[] data_horizontal = new double[buffsize];
        double left_vertical_min, left_vertical_max;
        double[] calibration_data_right_vertical = new double[buffsize];
        double[] calibration_data_horizontal = new double[buffsize];
        double[] saccade_calibration_data_right_vertical = new double[buffsize];
        double[] saccade_calibration_data_horizontal= new double[buffsize];
        static int realtime_data_buffsize = 300;
        double[] realtime_data_buffer_left_vertical = new double[realtime_data_buffsize];
        double[] realtime_data_buffer_right_vertical = new double[realtime_data_buffsize];
        double[] realtime_data_buffer_horizontal = new double[realtime_data_buffsize];
        double[] realtime_diff_data_buffer_left_vertical = new double[realtime_data_buffsize];
        double[] realtime_diff_data_buffer_right_vertical = new double[realtime_data_buffsize];
        double[] realtime_diff_data_buffer_horizontal = new double[realtime_data_buffsize];

        Queue<int> pos_delta_queue_x = new Queue<int>();
        Queue<int> pos_delta_queue_y = new Queue<int>();

        int saccade_top = 0;
        
        int face_on_flag = 0;
        int face_on_counter = 0;
        int face_pos_x = 0;
        int face_pos_y = 0;

        SignalProcess signal_processor;



        double horizontal_peak_max, horizontal_peak_min;
        double baseline_ratio = 0.2;
        double horizontal_positive_ratio = 0;
        double horizontal_negative_ratio = 0;
        double vertical_negative_ratio = 0;
        double vertical_positive_ratio = 0;

        double vertical_upper_threshold_value, vertical_lower_threshold_value;
        double horizontal_upper_threshold_value, horizontal_lower_threshold_value;
        double vertical_gaze_pos, horizontal_gaze_pos;
        double vertical_peak_max, vertical_peak_min;

        public struct EOG_position
        {
            public int x, y;
            public double EOG_left_vertical, EOG_right_vertical, EOG_horizontal;
        }

        EOG_position[] Cal_data = new EOG_position[5000];

        //Console
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public WAT()
        {
            InitializeComponent();
            AllocConsole();
            
        }
        private void ClearAndClosePort()
        {
            if (sPort.IsOpen)
            {
                string _ = sPort.ReadExisting();
                sPort.Close();
            }
        }

        public (int, int) Get_trackingBoxPosition()
        {
            return (trackingbox.Location.X, trackingbox.Location.Y);
        }
      

        private void btnCalibration_Click(object sender, EventArgs e)
        {
            btnReset.Visible = true;
            
            //Calibration 1 
            // 점 따라가기
            if (flag == 2) {
                sPort.Open();
                Tablepanel.Visible = false;
                Cal2text.Visible = false;
                move_black.BackColor = Color.Black;
                move_black.Size = new Size(30, 30);
                move_black.Visible = true;

                timer5.Enabled = true;
                

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
                //game pnt
                game_pnt_X = new int[10] { gamepnt1.Location.X, gamepnt2.Location.X, gamepnt3.Location.X, gamepnt4.Location.X, gamepnt5.Location.X, gamepnt6.Location.X, gamepnt7.Location.X, gamepnt8.Location.X, gamepnt9.Location.X, gamepnt10.Location.X};
                game_pnt_Y = new int[10] { gamepnt1.Location.Y, gamepnt2.Location.Y, gamepnt3.Location.Y, gamepnt4.Location.Y, gamepnt5.Location.Y, gamepnt6.Location.Y, gamepnt7.Location.Y, gamepnt8.Location.Y, gamepnt9.Location.Y, gamepnt10.Location.Y};

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
                trackingbox.BackColor = Color.White;
                trackingbox.Location = new Point(max_x / 2, max_y / 2);
                trackingbox.Size = new Size(30, 30);
                trackingbox.Visible = true;

                Tablepanel.Visible = true;

                ClearAndClosePort();
                sPort.Open();
                //sPort.Open();
                Game_Panel.Visible = true;
                game_timer.Enabled = true;
                img_cnt = 0;

                timer5.Enabled = true;
                Tablepanel.Visible=false;
                btnCalibration.Text = "End";
                
                vertical_gaze_pos = max_y / 2;
                horizontal_gaze_pos = max_x / 2;
                trackingbox.BringToFront();
                tracking_delay.Enabled = true;
                tracking_delay.Start();
                return;
            }

            // End
            if (flag == 4)
            {

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

        private void delay_done(object sender, EventArgs e)
        {
            game_start_flag = 1;
            tracking_delay.Enabled = false;
            tracking_delay.Stop();
            tracking_update_timer.Enabled = true;
            tracking_update_timer.Start();
        }

        private void update_TrackingPosition(object sender, EventArgs e)
        {
            trackingbox.Location = new Point((int)horizontal_gaze_pos, (int)vertical_gaze_pos);
        }

        private void gaze_location_update_timer(object sender, EventArgs e)
        {
            if (pos_delta_queue_x.Count <= 0) return;
            trackingbox.Location = new Point((int)horizontal_gaze_pos, (int)vertical_gaze_pos);

            //scope2.Channels[0].Data.SetYData(realtime_diff_data_buffer_right_vertical);
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

            //Console.Write("sac cal start");
            saccade_calibration();



        }
        
        private void blink_calibration()
        {
            double[] cal_data_right_vertical = calibration_data_right_vertical.Take(cali_top).ToArray();
            double[] vertical_diff_result = signal_processor.Differential(cal_data_right_vertical);
            double[] filtered_vertical_diff_signal = signal_processor.ButterworthHighpassLowpassFilter(vertical_diff_result, sampling_rate, 0.1, 1000, 2);
            double[] peak_detection_result, filtered_y;
            (peak_detection_result, filtered_y) = signal_processor.FindPeaks(filtered_vertical_diff_signal, lag, threshold, influence);
            (vertical_peak_max, vertical_peak_min) = signal_processor.PeakMinMax(filtered_vertical_diff_signal, peak_detection_result, lag);
            vertical_upper_threshold_value = baseline_ratio * vertical_peak_max;
            vertical_lower_threshold_value = baseline_ratio * vertical_peak_min;
            int[] blink_detection_result = signal_processor.BlinkDetection(filtered_vertical_diff_signal, vertical_peak_max, vertical_peak_min);

            //scope2.Channels[0].Data.SetYData(test_value);
            //scope2.Channels[0].Data.SetYData(filtered_vertical_diff_signal);
            scope3.Channels[0].Data.SetYData(blink_detection_result) ;
        }
        private void saccade_calibration()
        {
            saccade_calibration_data_right_vertical = saccade_calibration_data_right_vertical.Take(saccade_top).ToArray();
            double[] vertical_diff_result = signal_processor.Differential(saccade_calibration_data_right_vertical);
            //double[] filtered_vertical_diff_signal = signal_processor.ButterworthHighpassLowpassFilter(vertical_diff_result, sampling_rate, 0.1, 100, 2);
            int[] blink_detection_result = signal_processor.BlinkDetection(vertical_diff_result, vertical_peak_max, vertical_peak_min);
            double max_pos_eog_value = 0;
            double min_pos_eog_value = 0;
            double max_pos = -1;
            
            // state = 1 : UP state = 0 : Down
            double min_pos = 999999;
            double recent_eog = Cal_data[0].EOG_right_vertical;
            int touch_bottom = 0;
            int touch_top = -1;
            
            int vertical_negative_count = 0;
            
            int vertical_positive_count = 0;
            for(int i=5;i<saccade_top-1;i++)
            {
                //if (Convert.ToBoolean(signal_processor.CheckCrossThreshold(blink_removed_diff_signal[i], vertical_upper_threshold_value, vertical_lower_threshold_value)))
                if (blink_detection_result[i] == 0)
                {
                    // up
                    if (Cal_data[i+1].y - Cal_data[i].y < 0)
                    {
                        
                        if(touch_bottom == 0)
                        {
                            if(touch_top == -1)
                            {
                                touch_top = 0;
                            }
                            touch_bottom = 1;
                            min_pos = Cal_data[i].y;
                            min_pos_eog_value = Cal_data[i].EOG_right_vertical;
                        }
                        if (touch_top == 1)
                        {
                            touch_top = 0;
                            double pos_delta = max_pos - min_pos;
                            
                            double eog_delta = max_pos_eog_value - min_pos_eog_value;
                            vertical_negative_ratio += (pos_delta / eog_delta);
                            vertical_negative_count++;
                        }

                    }
                    // to down
                    else if(Cal_data[i + 1].y - Cal_data[i].y > 0)
                    {
                        if (touch_top == 0)
                        {
                            touch_top = 1;
                            max_pos = Cal_data[i].y;
                            max_pos_eog_value = Cal_data[i].EOG_right_vertical;
                        }
                        if (touch_bottom == 1)
                        {
                            touch_bottom = 0;
                            double pos_delta = min_pos - max_pos;
                            double eog_delta = min_pos_eog_value - max_pos_eog_value;

                           
                            vertical_positive_ratio += (pos_delta / eog_delta);
                            vertical_positive_count++;

                            
                        }
                    }
                   

                }
            }
            vertical_positive_ratio /= vertical_positive_count;
            vertical_negative_ratio /= vertical_negative_count;

            //Console.Write("positive ratio : ");
            //Console.WriteLine(vertical_positive_ratio);
            //Console.Write("negative ratio : ");
            //Console.WriteLine(vertical_negative_ratio);
           
            saccade_calibration_data_horizontal = saccade_calibration_data_horizontal.Take(saccade_top).ToArray();
            double[] horizontal_diff_result = signal_processor.Differential(saccade_calibration_data_horizontal);
            double[] horizontal_peak_detection_result, horizontal_filtered_y;
            (horizontal_peak_detection_result, horizontal_filtered_y)= signal_processor.FindPeaks(horizontal_diff_result, lag, threshold, influence);
            (horizontal_peak_max, horizontal_peak_min) = signal_processor.PeakMinMax(horizontal_diff_result, horizontal_peak_detection_result, lag);
            horizontal_upper_threshold_value = horizontal_peak_max * 0.5;
            horizontal_lower_threshold_value = horizontal_peak_min * 0.5;
            int touch_left = 0;
            int touch_right = -1;
            
            int horizontal_positive_count = 0;
            int horizontal_negative_count = 0;
            for (int i = 5; i < saccade_top-1; i++)
            {
                // to left
                if (Cal_data[i + 1].x - Cal_data[i].x < 0)
                {

                    if (touch_right == 0)
                    {
                        touch_right = 1;
                        max_pos = Cal_data[i].x;
                        max_pos_eog_value = Cal_data[i].EOG_horizontal;
                    }
                    if (touch_left == 1)
                    {
                        touch_left = 0;
                        double pos_delta = min_pos - max_pos;
                        double eog_delta = min_pos_eog_value - max_pos_eog_value;

                        horizontal_positive_ratio += (pos_delta / eog_delta);
                        horizontal_positive_count++;
                    }                    
                }
                // to right
                else if (Cal_data[i + 1].x - Cal_data[i].x > 0)
                {
                    if (touch_left == 0)
                    {
                        if(touch_right == -1)
                        {
                            touch_right = 0;
                        }
                        touch_left = 1;
                        min_pos = Cal_data[i].x;
                        min_pos_eog_value = Cal_data[i].EOG_horizontal;
                    }
                    if (touch_right == 1)
                    {
                        touch_right = 0;
                        double pos_delta = max_pos - min_pos;
                        double eog_delta = max_pos_eog_value - min_pos_eog_value;

                        horizontal_negative_ratio += (pos_delta / eog_delta);
                        horizontal_negative_count++;
                    }
                }

  
            }

            horizontal_positive_ratio /= horizontal_positive_count;
            horizontal_negative_ratio /= horizontal_negative_count;

            
            scope1.Channels[0].Data.SetYData(vertical_diff_result);
            scope3.Channels[0].Data.SetYData(horizontal_diff_result);

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
                            

                            EOG_position Calibration_Data = new EOG_position
                            {
                                x = X,
                                y = Y,
                                EOG_right_vertical = Data_1,
                                EOG_horizontal = Data_2
                            };

                            Cal_data[saccade_top] = Calibration_Data;
                            saccade_top++;
                            //count++;

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
                            double vertical_diff = Data_1 - realtime_data_buffer_right_vertical[realtime_data_buffsize - 2];
                            double horizontal_diff = Data_2 - realtime_data_buffer_horizontal[realtime_data_buffsize - 2];
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
                                if (face_on_counter >= delay_idx + 50)
                                {
                                    face_on_counter = 0;
                                    face_on_flag = 0;
                                }
                            }

                            int slice_start_idx = realtime_data_buffsize - (delay_idx + 2);
                            int slice_end_idx = realtime_data_buffsize - 1;
                            int wink_detected = 0;
                            int cross_threshold_flag = signal_processor.CheckCrossThreshold(
                                delayed_vertical_diff, 
                                vertical_peak_max * signal_processor.baseline_ratio,
                                vertical_peak_min * signal_processor.baseline_ratio
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
                                        //realtime_diff_data_buffer_horizontal[i] = 0;
                                    }
                                    if (blink_detection_result[i - slice_start_idx] == 2) wink_detected = 1;
                                }
                                if (face_on_flag == 1 && wink_detected == 1)
                                {
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
                                            int wink_idx_distance = realtime_data_buffsize - wink_start_idx;
                                            
                                            wink_period = (wink_end_idx - wink_start_idx) / sampling_rate;
                                            Console.Write("Wink Period : ");
                                            Console.WriteLine(wink_period);
                                            Console.Write("Wink Delayed idx : ");
                                            Console.WriteLine(wink_idx_distance);
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


                                            //vertical_gaze_pos = face_pos_y;
                                            //horizontal_gaze_pos = face_pos_x;

                                            current_state = 0;
                                            break;
                                        }
                                    }
                                }
                                wink_detected = 0;
                                
                            }

                           
                            double vertical_delta_pos;
                            if (delayed_vertical_diff <= 0)
                            {
                                vertical_delta_pos = vertical_negative_ratio * delayed_vertical_diff;
                            }
                            else
                            {
                                vertical_delta_pos = vertical_positive_ratio * delayed_vertical_diff;
                            }                                
                            vertical_gaze_pos += vertical_delta_pos;
                            if (vertical_gaze_pos > max_y) vertical_gaze_pos = max_y;
                            else if(vertical_gaze_pos < 0) vertical_gaze_pos = 0;

                            
                            double horizontal_delta_pos;
                            if (delayed_horizontal_diff <= 0)
                            {
                                horizontal_delta_pos = horizontal_negative_ratio * delayed_horizontal_diff;
                            }
                            else
                            {
                                horizontal_delta_pos = horizontal_positive_ratio * delayed_horizontal_diff;
                            }
                            horizontal_gaze_pos += horizontal_delta_pos;
                            if (horizontal_gaze_pos > max_x) horizontal_gaze_pos = max_x;
                            else if (horizontal_gaze_pos < 0) horizontal_gaze_pos = 0;

                            
                            

                        }
                        start_flag = 0;

                    }
                }
                


            }

        }

        private void On_timer(object sender, EventArgs e)
        {
            //scope2.Channels[0].Data.SetYData(calibration_data_right_vertical);
            //scope3.Channels[0].Data.SetYData();
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
            ClearAndClosePort();
            timer5.Enabled = false;
            timer5.Stop();
            calibration_start_flag = 0;

            btnNext.Visible = true;
            move_black.Visible = false;
            timer3.Enabled = false;
            timer4.Enabled = false;
            signal_processor = new SignalProcess(calibration_data_right_vertical);
            
            
            blink_calibration();
            //Console.Write("Cal1 end");

        }
    }
}

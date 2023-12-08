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

        //EOG data
        public int[] data_buff = new int[1200];
        public static int buffsize = 1200;
        public double[] data_left_vertical = new double[buffsize];
        public double[] data_right_vertical = new double[buffsize];
        public double[] data_horizontal = new double[buffsize];

        public double[] input_Draw_1 = new double[buffsize];
        public double[] input_Draw_2 = new double[buffsize];
        public double[] input_Draw_3 = new double[buffsize];

        public int start_byte = 0;
        public int start_flag = 0;
        public int data_count = 0;
        public int Data_1, Data_2, Data_3;
        public int count = 0;

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
            if (flag == 1) {
                sPort.Open();
                timer5.Enabled = true;

                max_x = move_black.Location.X;
                max_y = move_black.Location.Y;
                move_black.Visible = true;

                angle = 0;
                timer1.Enabled = true;
                timer2.Enabled = true;

                timer1.Start();
                timer2.Start();
            }

            //Calicbration 2 
            if (flag == 2)
            {
                sPort.Open();
                timer5.Enabled = true;

                Tablepanel.Visible = false;
                timer3.Enabled = true;
                timer4.Enabled = true;
                Cal2text.Visible = true;
                move_black.Location = new Point(max_x/2, max_y/2);
                move_black.Visible = true;
                move_black.Size = new Size(50, 50);
            }

            // game Start
            if (flag == 3)
            {
                sPort.Open();
                timer5.Enabled = true;

                Tablepanel.Visible=false;
                btnCalibration.Text = "End";
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
            if (flag == 1)
            {
                sPort.Close();
                timer5.Enabled = false;

                Tablepanel.Visible = false;
                timer1.Enabled = false;
                timer2.Enabled = false;
                move_black.Visible = false;

                move_black.Location = new Point(max_x, max_y);
            }

            //Calibration2
            if (flag == 2)
            {
                sPort.Close();
                timer5.Enabled = false;

                Tablepanel.Visible = false;
                timer3.Enabled = false;
                timer4.Enabled = false;
                move_black.BackColor = Color.Black;
            }
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
            
           
            timer5.Enabled = false;
            timer5.Stop();

            timer1.Enabled = false;
            timer1.Stop();
            btnNext.Visible = true;
            timer2.Enabled = false;
            timer2.Stop();
            //sPort.Close();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            btnNext.Visible = false;
            move_black.Visible = false;

            if (flag == 1)
            {
                btnCalibration.Text = "Calibration2";
                Tablepanel.Visible = true;
            }

            if (flag == 2)
            {
                Cal2text.Visible = false;
                btnReset.Visible = false;
                btnCalibration.Text = "Game Start";
                Tablepanel.Visible = true;
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

                    if (data_count == 5)
                    {
                        Data_1 = ((data_buff[0] & 0x7f) << 7) + (data_buff[1] & 0x7f);
                        Data_2 = ((data_buff[2] & 0x7f) << 7) + (data_buff[3] & 0x7f);
                        Data_3 = ((data_buff[4] & 0x7f) << 7) + (data_buff[5] & 0x7f);

                        start_flag = 2;
                        data_count = 0;
                    }


                    if (start_flag == 2)
                    {
                        Data_1 -= 7000;
                        Data_2 -= 7000;
                        Data_3 -= 7000;
                        for ( int i = 0; i < buffsize - 1; i++)
                        {
                            data_left_vertical[i] = data_left_vertical[i + 1];
                            data_right_vertical[i] = data_right_vertical[i + 1];
                            data_horizontal[i] = data_horizontal[i + 1];
                        }

                        data_left_vertical[buffsize - 1] = Data_1;
                        input_Draw_1 = data_left_vertical;

                        data_right_vertical[buffsize - 1] = Data_2;
                        input_Draw_2 = data_right_vertical;

                        data_horizontal[buffsize - 1] = Data_3 ;
                        input_Draw_3 = data_horizontal;

                        EOG_position Calibration_Data = new EOG_position
                        {
                            x = X,
                            y = Y,
                            EOG_left_vertical = Data_1,
                            EOG_right_vertical = Data_2,
                            EOG_horizontal = Data_3
                        };

                        Cal_data[count] = Calibration_Data;
                        Console.Write(Cal_data[count].EOG_right_vertical);
                        Console.Write("  ");
                        Console.Write(Cal_data[count].EOG_left_vertical);
                        Console.Write("  ");
                        Console.Write(Cal_data[count].EOG_horizontal);
                        Console.Write("  ");
                        Console.Write(Cal_data[count].x);
                        Console.Write("  ");
                        Console.WriteLine(Cal_data[count].y);
                       

                        count++;

                        start_flag = 0;
                    }
                }

            }

        }

        private void On_timer(object sender, EventArgs e)
        {
            scope1.Channels[0].Data.SetYData(data_left_vertical);
            scope2.Channels[0].Data.SetYData(data_right_vertical);
            scope3.Channels[0].Data.SetYData(data_horizontal);
        }

        private void Calibration2_Timer(object sender, EventArgs e)
        {
            
            int red = Convert.ToInt16(cnt%3==0)*255;
            int green = Convert.ToInt16(cnt % 3 == 1) * 255;
            int blue = Convert.ToInt16(cnt % 3 == 2) * 255;

            cnt++;

            move_black.BackColor = Color.FromArgb(red, green, blue);
        }

        private void Calibration2_stop_timer(object sender, EventArgs e)
        {
            sPort.Close();
            timer5.Enabled = false;
            timer5.Stop();

            btnNext.Visible = true;
            move_black.Visible = false;
            timer3.Enabled = false;
            timer4.Enabled = false;
        }
    }
}

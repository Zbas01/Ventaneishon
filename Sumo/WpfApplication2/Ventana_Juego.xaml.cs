using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{
    using Newtonsoft.Json;
    using System.Net;
    using System.Net.Sockets;
    using System.Windows.Threading;
    enum Difficulty
            {
                Easy = 1,
                Normal = 5,
                Hard = 10,
                Insane = 20,
            }
    class GamePacket
    {
        public int PlayerNo = 1;
        public bool SmallBalls = false;
        public bool aShock = false;
        public bool noTouch = false;
        public double Level = (double)Difficulty.Normal;
        public Point Position = new Point(400, 300);
        public Point Speed = new Point(0, 0);

        public bool Died = false;
        public bool Ready = false;
    }
    public partial class Window1 : Window
    {
        //Conexión
        GamePacket Sending = new GamePacket();     //Yo
        GamePacket Recieving = new GamePacket();   //El

        UdpClient GameSocket;
        IPAddress ipa;
        int GamePort = 7707;
        int PlayerNo = 1;
        int Round = 0;

        //Configuracion
        bool SmallBalls = false;
        bool aShock = false;
        bool noTouch = false;
        double Level = (double)Difficulty.Normal;

        //Vectores: Velocidad y Posición
        private Point SpeedA = new Point(0, 0);
        private Point SpeedB = new Point(0, 0);
        private Point PositionA = new Point(300, 300);
        private Point PositionB = new Point(500, 300);
        private Point Ring = new Point(400, 300);

        //Score
        int ScoreA = 0;
        int ScoreB = 0;

        //Fisica y tamaño
        private double Accel = .1;
        private double Friction = .9;
        private double M1 = 32;
        private double M2 = 32;
        private double RingRad = 200;

        private double RoundTimeout = 4;

        //Cuenta regresiva para activar la electricidad del ring
        double BadCounter = 1;

        //Controles
        //bool up = false, down = false, left = false, right = false;
        bool up = false, left = false, down = false, right = false;

        DispatcherTimer timer = new DispatcherTimer();
        public Window1()
        {
            //Window1(IP, Numero de jugador)


            InitializeComponent();
            //Inicializa la configuracion
            initConnection("127.0.0.1", GamePort);

            //Inicializa los eventos
            KeyDown += new KeyEventHandler(OnButtonKeyDown);
            KeyUp += new KeyEventHandler(OnButtonKeyUp);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0,0,0,0,8); 
            timer.Start();

            //Inicializa el dibujo
            paintCircle(Ring, Brushes.Gray, RingRad);
            paintCircle(PositionA, Brushes.Red, M1);
            paintCircle(PositionB, Brushes.Blue, M2);
        }
        private void initConnection(string ip, int port)
        {
            ipa = IPAddress.Parse(ip);
            GameSocket = new UdpClient(port, AddressFamily.InterNetwork);
        }
        private void getConnection() {
            IPEndPoint getConn = new IPEndPoint(ipa, GamePort);
            if (GameSocket.Available > 0)
            {
                byte[] receiveBytes = GameSocket.Receive(ref getConn);
                Recieving = JsonConvert.DeserializeObject<GamePacket>(Encoding.ASCII.GetString(receiveBytes));
                if (Recieving.PlayerNo != 0) {
                    if (Recieving.PlayerNo == 1 && PlayerNo != Recieving.PlayerNo)
                    {
                        if (PlayerNo == 0)
                            PlayerNo = 2;
                        Level = Recieving.Level;
                        aShock = Recieving.aShock;
                        noTouch = Recieving.noTouch;
                        SmallBalls = Recieving.SmallBalls;
                    }
                    PositionB = Recieving.Position;
                    PositionB.X = 800 - PositionB.X;
                    SpeedB = Recieving.Speed;
                    SpeedB.X *= -2;
                    SpeedB.Y *= 2;
                }
            }
        }
        private void setConnection()
        {
            IPEndPoint setConn = new IPEndPoint(ipa, GamePort);
            UdpClient tempSocket = new UdpClient();
            string test = JsonConvert.SerializeObject(Sending);
            byte[] sBytes = Encoding.ASCII.GetBytes(test);

            Sending.PlayerNo = PlayerNo;
            Sending.Position = PositionA;
            Sending.Speed = SpeedA;

            if (PlayerNo == 1) {
                //Modo de juego
                Sending.Level = Level;
                Sending.aShock = aShock;
                Sending.noTouch = noTouch;
                Sending.SmallBalls = SmallBalls;
            }
            tempSocket.Send(sBytes, sBytes.Length, setConn);
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            setConnection();
            getConnection();

            Title = "Player1:" + ScoreA.ToString() + ", Player2: " + ScoreB.ToString();


            if (!Sending.Ready || !Recieving.Ready || Sending.Died || Recieving.Died)
            {
                textP1Points.Text = ScoreA.ToString();
                textP2Points.Text = ScoreB.ToString();
                if (Sending.Ready)
                    textP1Ready.Text = "Listo";
                else
                    textP1Ready.Text = "Esperando";

                if (Recieving.Ready)
                    textP2Ready.Text = "Listo";
                else
                    textP2Ready.Text = "Esperando";
                return;
            }
            else
            {
                textP1Ready.Text = "";
                textP2Ready.Text = "";
            }

            if (RoundTimeout > 0)
            {
                RoundTimeout -= .02;
                if (RoundTimeout > 1)
                    textTimeout.Text = ((int)RoundTimeout).ToString();
                else
                    textTimeout.Text = "FIGHT!";
                paintCanvas.Children.Remove(textTimeout);
                paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
                paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
                paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
                paintCanvas.Background = Brushes.Black;

                if (aShock)
                    paintCircle(Ring, Brushes.Green, RingRad);
                else
                    paintCircle(Ring, Brushes.Gray, RingRad);

                if (PlayerNo == 1)
                {
                    paintCircle(PositionA, Brushes.Red, M1);
                    paintCircle(PositionB, Brushes.Blue, M2);
                }
                else
                {
                    paintCircle(PositionA, Brushes.Blue, M1);
                    paintCircle(PositionB, Brushes.Red, M2);
                }

                paintCanvas.Children.Insert(paintCanvas.Children.Count, textTimeout);
                if (RoundTimeout > 1)
                    return;
            }
            else
                textTimeout.Text = "";

            Accel += .0005 * Level;
            BadCounter -= .01;
            //Se aplica el movimiento
            //JugadorA
            if (up)
                SpeedA.Y -= Accel;
            if (down)
                SpeedA.Y += Accel;
            if (left)
                SpeedA.X -= Accel;
            if (right)
                SpeedA.X += Accel;
            //JugadorB
            //if (up)
            //    SpeedB.Y -= Accel;
            //if (down)
            //    SpeedB.Y += Accel;
            //if (left)
            //    SpeedB.X -= Accel;
            //if (right)
            //    SpeedB.X += Accel;

            //Aplica la velocidad
            PositionA.X += SpeedA.X;
            PositionA.Y += SpeedA.Y;
            PositionB.X += SpeedB.X;
            PositionB.Y += SpeedB.Y;

            //Colisiones
            if (distance(PositionA, PositionB) < (M1 + M2))
            {
                //Ctrl + Z
                PositionA.X -= SpeedA.X;
                PositionA.Y -= SpeedA.Y;
                PositionB.X -= SpeedB.X;
                PositionB.Y -= SpeedB.Y;

                SpeedB.X *= .5;
                SpeedB.Y *= .5;

                if (noTouch)
                {
                    timer.IsEnabled = false;
                    paintCanvas.Background = Brushes.YellowGreen;
                    if (ScoreA > 0)
                        ScoreA--;
                    if (ScoreB > 0)
                        ScoreB--;
                    return;
                }
                //Colision en 1D
                Point Tmp = SpeedA;
                SpeedA.X = (((M1 - M2) /
                            (M1 + M2)) * SpeedA.X)
                            +
                            (((M2 * 2) /
                            (M1 + M2)) * SpeedB.X);

                SpeedB.X = (((M1 * 2) /
                            (M1 + M2)) * Tmp.X)
                            +
                            (((M2 - M1) /
                            (M1 + M2)) * SpeedB.X);

                SpeedA.Y = (((M1 - M2) /
                            (M1 + M2)) * SpeedA.Y)
                            +
                            (((M2 * 2) /
                            (M1 + M2)) * SpeedB.Y);

                SpeedB.Y = (((M1 * 2) /
                            (M1 + M2)) * Tmp.Y)
                            +
                            (((M2 - M1) /
                            (M1 + M2)) * SpeedB.Y);

                //Colision en 2D
                double ang = angle(PositionA, PositionB);
                Tmp.X = (1 + Level / 10) * (SpeedA.X * Math.Abs(Math.Cos(ang)) - SpeedA.Y * Math.Sin(ang) * Math.Cos(ang));
                Tmp.Y = (1 + Level / 10) * ((SpeedA.Y * Math.Abs(Math.Sin(ang)) - SpeedA.X * Math.Cos(ang) * Math.Sin(ang)));
                SpeedA = Tmp;

                Tmp.X = (1 + Level / 10) * (SpeedB.X * Math.Abs(Math.Cos(ang)) - SpeedB.Y * Math.Sin(ang) * Math.Cos(ang));
                Tmp.Y = (1 + Level / 10) * ((SpeedB.Y * Math.Abs(Math.Sin(ang)) - SpeedB.X * Math.Cos(ang) * Math.Sin(ang)));
                SpeedB = Tmp;

                //Reaplica la velocidad
                PositionA.X += SpeedA.X;
                PositionA.Y += SpeedA.Y;
                PositionB.X += SpeedB.X;
                PositionB.Y += SpeedB.Y;
            }
            //Aplica la friccion
            SpeedA.X *= Friction;
            SpeedA.Y *= Friction;
            //SpeedB.X *= Friction;
            //SpeedB.Y *= Friction;

            //Limpia la escena
            paintCanvas.Children.Remove(textTimeout);
            paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
            paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
            paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);

            //Dibuja el ring
            if (aShock)
                if (BadCounter < 0)
                    paintCircle(Ring, Brushes.YellowGreen, RingRad);
                else
                    paintCircle(Ring, Brushes.Green, RingRad);
            else
                paintCircle(Ring, Brushes.Gray, RingRad);

            //Dibuja los jugadores
            if (PlayerNo == 1)
            {
                paintCircle(PositionA, Brushes.Red, M1);
                paintCircle(PositionB, Brushes.Blue, M2);
            }
            else
            {
                paintCircle(PositionA, Brushes.Blue, M1);
                paintCircle(PositionB, Brushes.Red, M2);
            }

            paintCanvas.Children.Insert(paintCanvas.Children.Count, textTimeout);
            //Checa si alguien esta en la orilla
            double disA = distance(PositionA, Ring);
            double disB = distance(PositionB, Ring);
            if (disA < RingRad + M1)
            {
                SpeedA.X -= ((Ring.X - PositionA.X) - M1) * Accel / 1000;
                SpeedA.Y -= ((Ring.Y - PositionA.Y) - M1) * Accel / 1000;
            }
            //if (disB < RingRad + M2)
            //{
            //    SpeedB.X -= ((Ring.X - PositionB.X) - M2) * Accel / 1000;
            //    SpeedB.Y -= ((Ring.Y - PositionB.Y) - M2) * Accel / 1000;
            //}

            //Aplica los shocks
            if (BadCounter < 0 && aShock)
            {
                if (disA < RingRad + M1)
                {
                    SpeedA.X *= 2;
                    SpeedA.Y *= 2;
                    BadCounter = 1;
                }
                //if (disB < RingRad + M2)
                //{
                //    SpeedB.X *= 2;
                //    SpeedB.Y *= 2;
                //    BadCounter = 1;
                //}
            }

            //Checa si nadie esta fuera del ring
            if (disA < 200 + M1)
                if (!Recieving.Died)
                    paintCanvas.Background = Brushes.Black;
                else
                {
                    Sending.Ready = false;
                    ScoreA++;
                    paintCanvas.Background = Brushes.DarkRed;
                }
            else
            {
                Sending.Ready = false;
                Sending.Died = true;
                ScoreB++;
                paintCanvas.Background = Brushes.DarkBlue;
            }

            //if (paintCanvas.Background != Brushes.Black)
            //{
            //    setConnection();
            //    //timer.IsEnabled = false;
            //}
        }
         #region "MATH.ZBAS"
                double angle (Point v1, Point v2)
                {
                    double xDiff = v2.X - v1.X;
                    double yDiff = v2.Y - v1.Y;
                    xDiff = -Math.Atan2(yDiff, xDiff);
                    if (xDiff < 0)
                        return xDiff + Math.PI*2;
                    else
                        return xDiff;
                }
                double distance(Point p1, Point p2)
                {
                    return distance(p1.X, p2.X, p1.Y, p2.Y);
                }
                double distance(double x1, double x2, double y1, double y2)
                {
                    return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
                }
        #endregion

        private void paintCircle(Point currentposition, Brush Color, double radious)
        {
            Ellipse newEllipse = new Ellipse();
            newEllipse.Fill = Color;
            newEllipse.Width = radious * 2;
            newEllipse.Height = radious * 2;

            Canvas.SetTop(newEllipse, currentposition.Y - radious);
            Canvas.SetLeft(newEllipse, currentposition.X - radious);
            paintCanvas.Children.Add(newEllipse);
        }
        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (!Sending.Ready || Sending.Died)
                switch (e.Key)
                {
                    case Key.Space:
                        Round++;
                        RoundTimeout = 4;
                        textRound.Text = Round.ToString();

                        BadCounter = 1;

                        PositionA = new Point(400 - 100, 300);
                        PositionB = new Point(400 + 100, 300);
                        bool tmp = Recieving.Ready;

                        Recieving = new GamePacket();
                        Recieving.Position = PositionB;
                        Recieving.Ready = tmp;
                        Sending = new GamePacket();
                        Sending.Ready = true;

                        SpeedA.X = 0;
                        SpeedA.Y = 0;

                        SpeedB.X = 0;
                        SpeedB.Y = 0;

                        Accel = .1;

                        up = false;
                        down = false;
                        left = false;
                        right = false;
                        up = false;
                        left = false;
                        down = false;
                        right = false;
                        break;
                    case Key.F1:
                        aShock = !aShock;
                        MessageBox.Show("Shock Mode = " + aShock.ToString());
                        break;
                    case Key.F2:
                        noTouch = !noTouch;
                        MessageBox.Show("Do not touch = " + noTouch.ToString());
                        break;
                    case Key.F3:
                        SmallBalls = !SmallBalls;
                        if (SmallBalls) {
                            M1 = 8;
                            M2 = 8;
                        } else
                        {
                            M1 = 32;
                            M2 = 32;
                        }
                        MessageBox.Show("SmallBalls = " + SmallBalls.ToString());
                        break;
                    case Key.Up:
                        switch ((int)Level) {
                            case (int)Difficulty.Easy:
                                Level = (double)Difficulty.Normal;
                                MessageBox.Show("Difficulty = Normal");
                                break;
                            case (int)Difficulty.Normal:
                                Level = (double)Difficulty.Hard;
                                MessageBox.Show("Difficulty = Hard");
                                break;
                            case (int)Difficulty.Hard:
                                Level = (double)Difficulty.Insane;
                                MessageBox.Show("Difficulty = Insane");
                                break;
                            }
                        break;
                    case Key.Down:
                        switch ((int)Level)
                        {
                            case (int)Difficulty.Normal:
                                Level = (double)Difficulty.Easy;
                                MessageBox.Show("Difficulty = Easy");
                                break;
                            case (int)Difficulty.Hard:
                                Level = (double)Difficulty.Normal;
                                MessageBox.Show("Difficulty = Normal");
                                break;
                            case (int)Difficulty.Insane:
                                Level = (double)Difficulty.Hard;
                                MessageBox.Show("Difficulty = Hard");
                                break;
                        }
                        break;
                }
            switch (e.Key)
            {
                case Key.A:
                    SpeedA.X -= Accel * 2;
                    left = true;
                    break;
                case Key.D:
                    SpeedA.X += Accel * 2;
                    right = true;
                    break;
                case Key.S:
                    SpeedA.Y += Accel * 2;
                    down = true;
                    break;
                case Key.W:
                    SpeedA.Y -= Accel * 2;
                    up = true;
                    break;

                //case Key.Left:
                //    SpeedB.X -= Accel * 2;
                //    left = true;
                //    break;
                //case Key.Right:
                //    SpeedB.X += Accel * 2;
                //    right = true;
                //    break;
                //case Key.Down:
                //    SpeedB.Y += Accel * 2;
                //    down= true;
                //    break;
                //case Key.Up:
                //    SpeedB.Y -= Accel * 2;
                //    up= true;
                //    break;
            }
        }
        private void OnButtonKeyUp(object sender, KeyEventArgs e)
        {
                switch (e.Key)
                {
                    case Key.W:
                        up = false;
                        break;
                    case Key.A:
                        left = false;
                        break;
                    case Key.D:
                        right = false;
                        break;
                    case Key.S:
                        down = false;
                        break;
                    //case Key.Left:
                    //    left = false;
                    //    break;
                    //case Key.Right:
                    //    right = false;
                    //    break;
                    //case Key.Down:
                    //    down = false;
                    //    break;
                    //case Key.Up:
                    //    up = false;
                    //    break;
                }
        }
    }
}


using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;

namespace WpfApplication1
{
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
        public double Level = (double)Difficulty.Normal;

        public Point Position;
        public Point Speed;

        public Point PositionB;
        public Point SpeedB;

        public Point Score;
        public int Round;       //Denota el Ready
        public int Dead;

        public bool SmallBalls = false;
        public bool aShock = false;
        public bool noTouch = false;

        public bool Up, Down, Left, Right;

        public void clearRound() //Resetea el paquete para nueva ronda
        {
            Round++;
            Position.X = 300;
            Position.Y = 300 + 100*PlayerNo;
            Speed.X = 0;
            Speed.Y = 0;
            Up = false;
            Down = false;
            Left = false;
            Right = false;
            Dead = 0;       //Ready
        }
        public void clearMatch() //Resetea el paquete para revancha
        {
            Score.X = 0;
            Score.Y = 0;
            Round = -1;
            clearRound();
        }

    }
    public partial class Window1 : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        GamePacket A = new GamePacket(); //Sending
        GamePacket B = new GamePacket(); //Recieving
        UdpClient GameSocket;
        IPAddress ipa;
        int GamePort = 7707;
        bool GameExit = false;

        //Timeouts
        private double RoundTimeout = 4;
        double ShockCount = 1;

        //Fisica y tamaño
        private double Accel = .1;
        private double Friction = .9;
        private double M1 = 32;
        private double M2 = 32;
        private double RingRad = 200;
        private Point Ring = new Point(400, 300);

        Brush ColorA = Brushes.Red;
        Brush ColorB = Brushes.Blue;

        public Window1()
        {
            //Window1(IP, Numero de jugador)
            InitializeComponent();
            //Inicializa los eventos
            KeyDown += new KeyEventHandler(OnButtonKeyDown);
            KeyUp += new KeyEventHandler(OnButtonKeyUp);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0,0,0,0,8); 
            timer.Start();

            //Inicializa
            drawScene(false);
            initConnection("127.0.0.1", GamePort);
        }

        private void initConnection(string ip, int port)
        {
            ipa = IPAddress.Parse(ip);
            GameSocket = new UdpClient(port, AddressFamily.InterNetwork);
            Thread connThread = new Thread(getConnection);
            connThread.Start();
        }
        private void drawScene(bool clear)
        {
            if (clear)
            {
                paintCanvas.Children.Remove(textTimeout);
                paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
                paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
                paintCanvas.Children.RemoveAt(paintCanvas.Children.Count - 1);
            }
            paintCanvas.Background = Brushes.Black;

            if (A.PlayerNo == 1)
            {
                ColorA = Brushes.Red;
                ColorB = Brushes.Blue;
            }
            else {
                ColorA = Brushes.Blue;
                ColorB = Brushes.Red;
            }

            if (A.aShock)
                if (ShockCount < 0)
                     paintCircle(Ring, Brushes.Green        , RingRad);
                else paintCircle(Ring, Brushes.YellowGreen  , RingRad);
            else     paintCircle(Ring, Brushes.Gray         , RingRad);

            paintCircle(A.Position, ColorA, M1);
            paintCircle(B.Position, ColorB, M2);
            paintCanvas.Children.Insert(paintCanvas.Children.Count, textTimeout);
        }

        private void getConnection() {
            IPEndPoint getConn = new IPEndPoint(ipa, GamePort);
            while(!GameExit)
                if (GameSocket.Available > 0)
                {
                    byte[] receiveBytes = GameSocket.Receive(ref getConn);
                    B = JsonConvert.DeserializeObject<GamePacket>(Encoding.ASCII.GetString(receiveBytes));

                    if (A.PlayerNo != 1)
                    {
                        A.Position = B.PositionB;
                        A.Speed = B.SpeedB;
                        A.PositionB = B.Position;
                        A.SpeedB = B.Speed;

                        A.Position.X = 800 - A.PositionB.X; //Lo pone en la derecha
                        A.PositionB.X = 800 - A.PositionB.X; //Lo pone en la izquierda
                        A.SpeedB.X *= -1;
                        A.Speed.X *= -1;

                        A.Score = B.Score;

                        A.Round = B.Round;
                        A.Level = B.Level;

                        A.aShock = B.aShock;
                        A.noTouch = B.noTouch;
                        A.SmallBalls = B.SmallBalls;
                    }
                }
        }
        private void setConnection()
        {
            IPEndPoint setConn = new IPEndPoint(ipa, GamePort);
            UdpClient tempSocket = new UdpClient();
            string test = JsonConvert.SerializeObject(A);
            byte[] sBytes = Encoding.ASCII.GetBytes(test);
            tempSocket.Send(sBytes, sBytes.Length, setConn);
        }

        private void serverSide()
        {
            textP1Points.Text = A.Score.X.ToString();
            textP2Points.Text = A.Score.Y.ToString();
            if (A.SmallBalls)
                M1 = 8;
            else
                M1 = 32;
            M2 = M1;
            switch (A.Dead) {
                case 1:
                    A.Dead = -1;
                    A.Score.X++;
                    paintCanvas.Background = Brushes.DarkRed;
                    break;
                case 2:
                    A.Dead = -1;
                    A.Score.Y++;
                    paintCanvas.Background = Brushes.DarkBlue;
                    break;
                case 3:
                    A.Dead = -1;
                    paintCanvas.Background = Brushes.DarkGreen;
                    break;
                case 0:
                    paintCanvas.Background = Brushes.Black;
                    break;
            }
        }
        private void clientSide()
        {
            textP1Points.Text = B.Score.Y.ToString();
            textP2Points.Text = B.Score.X.ToString();
            if (B.SmallBalls)
                M1 = 8;
            else
                M1 = 32;
            M2 = M1;

            drawScene(true);
            if (A.Dead != B.Dead && A.Dead != -1)
            switch (B.Dead)
            {
                case 1:
                    A.Dead = -1;
                    paintCanvas.Background = Brushes.DarkRed;
                    break;
                case 2:
                    A.Dead = -1;
                    paintCanvas.Background = Brushes.DarkBlue;
                    break;
                case 3:
                    A.Dead = -1;
                    paintCanvas.Background = Brushes.DarkGreen;
                    break;
                case 0:
                    paintCanvas.Background = Brushes.Black;
                    break;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            setConnection();
            textRound.Text = A.Round.ToString();

            if (A.PlayerNo == 1)
                serverSide();
            else
                clientSide();

            if (A.Dead != 0 || B.Dead != 0) { drawScene(true); return; }

            Accel += .0005 * A.Level;
            ShockCount -= .01;

            //Se aplica el movimiento
            if (A.Up)       A.Speed.Y -= Accel * 3;
            if (A.Down)     A.Speed.Y += Accel * 3;
            if (A.Left)     A.Speed.X -= Accel * 3;
            if (A.Right)    A.Speed.X += Accel * 3;

            if (A.PlayerNo == 1)
            {
                if (B.Up)       A.SpeedB.Y -= Accel * 3;
                if (B.Down)     A.SpeedB.Y += Accel * 3;
                if (B.Left)     A.SpeedB.X += Accel * 3;
                if (B.Right)    A.SpeedB.X -= Accel * 3;
            }

            //Aplica la velocidad
            A.Position.X += A.Speed.X;
            A.Position.Y += A.Speed.Y;
            A.PositionB.X += A.SpeedB.X;
            A.PositionB.Y += A.SpeedB.Y;

            //Colisiones
            if (distance(A.Position, A.PositionB) < (M1 + M2))
            {
                //Ctrl + Z
                A.Position.X -= A.Speed.X;
                A.Position.Y -= A.Speed.Y;
                A.PositionB.X -= A.SpeedB.X;
                A.PositionB.Y -= A.SpeedB.Y;
                

                if (A.noTouch && A.PlayerNo == 1)
                {
                    A.Dead = 3;
                    return;
                }
                //Colision en 1D
                Point Tmp = A.Speed;
                A.Speed.X = (((M1 - M2) /
                            (M1 + M2)) * A.Speed.X)
                            +
                            (((M2 * 2) /
                            (M1 + M2)) * A.SpeedB.X);

                A.SpeedB.X = (((M1 * 2) /
                            (M1 + M2)) * Tmp.X)
                            +
                            (((M2 - M1) /
                            (M1 + M2)) * A.SpeedB.X);

                A.Speed.Y = (((M1 - M2) /
                            (M1 + M2)) * A.Speed.Y)
                            +
                            (((M2 * 2) /
                            (M1 + M2)) * A.SpeedB.Y);

                A.SpeedB.Y = (((M1 * 2) /
                            (M1 + M2)) * Tmp.Y)
                            +
                            (((M2 - M1) /
                            (M1 + M2)) * A.SpeedB.Y);

                //Colision en 2D
                double ang = angle(A.Position, A.PositionB);
                Tmp.X = (1 + A.Level / 10) * (A.Speed.X * Math.Abs(Math.Cos(ang)) - A.Speed.Y * Math.Sin(ang) * Math.Cos(ang));
                Tmp.Y = (1 + A.Level / 10) * ((A.Speed.Y * Math.Abs(Math.Sin(ang)) - A.Speed.X * Math.Cos(ang) * Math.Sin(ang)));
                A.Speed = Tmp;

                Tmp.X = (1 + A.Level / 10) * (A.SpeedB.X * Math.Abs(Math.Cos(ang)) - A.SpeedB.Y * Math.Sin(ang) * Math.Cos(ang));
                Tmp.Y = (1 + A.Level / 10) * ((A.SpeedB.Y * Math.Abs(Math.Sin(ang)) - A.SpeedB.X * Math.Cos(ang) * Math.Sin(ang)));
                A.SpeedB = Tmp;

                //Reaplica la velocidad
                A.Position.X += A.Speed.X;
                A.Position.Y += A.Speed.Y;
                A.PositionB.X += A.SpeedB.X;
                A.PositionB.Y += A.SpeedB.Y;
            }
            //Aplica la friccion
            A.Speed.X *= Friction;
            A.Speed.Y *= Friction;
            A.SpeedB.X *= Friction;
            A.SpeedB.Y *= Friction;

            //Dibuja todo
            drawScene(true);

            //Checa si alguien esta en la orilla
            double disA = distance(A.Position, Ring);
            double disB = distance(A.PositionB, Ring);
            if (disA < RingRad + M1)
            {
                A.Speed.X -= ((Ring.X - A.Position.X) - M1) * Accel / 1000;
                A.Speed.Y -= ((Ring.Y - A.Position.Y) - M1) * Accel / 1000;
            }
            if (disB < RingRad + M2)
            {
                A.SpeedB.X -= ((Ring.X - A.PositionB.X) - M2) * Accel / 1000;
                A.SpeedB.Y -= ((Ring.Y - A.PositionB.Y) - M2) * Accel / 1000;
            }

            //Aplica los shocks
            if (ShockCount < 0 && A.aShock)
            {
                if (disA < RingRad + M1)
                {
                    A.Speed.X *= 2;
                    A.Speed.Y *= 2;
                    ShockCount = 1;
                }
                if (disB < RingRad + M2)
                {
                    A.SpeedB.X *= 2;
                    A.SpeedB.Y *= 2;
                    ShockCount = 1;
                }
            }

            //Checa si nadie esta fuera del ring
            if (A.PlayerNo == 1)
            {
                if (disA < 200 + M1)
                    if (disB < 200 + M1)
                         A.Dead = 3; //Empate
                    else A.Dead = 1; //Azul gana
                else 
                if (disB < 200 + M1)
                    A.Dead = 2; //Rojo Gana
            }
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
            if (A.Dead != 0 && A.Dead != -1)
                switch (e.Key)
                {
                    case Key.Space:
                        RoundTimeout = 4;
                        ShockCount = 1;
                        Accel = .1;

                        A.clearRound();
                        A.clearRound();
                        break;
                    case Key.F1:
                        A.aShock = !A.aShock;
                        MessageBox.Show("Shock Mode = " + A.aShock.ToString());
                        break;
                    case Key.F2:
                        A.noTouch = !A.noTouch;
                        MessageBox.Show("Do not touch = " + A.noTouch.ToString());
                        break;
                    case Key.F3:
                        A.SmallBalls = !A.SmallBalls;
                        MessageBox.Show("SmallBalls = " + A.SmallBalls.ToString());
                        break;

                    case Key.Up:
                        switch ((int)A.Level) {
                            case (int)Difficulty.Easy:
                                A.Level = (double)Difficulty.Normal;
                                break;
                            case (int)Difficulty.Normal:
                                A.Level = (double)Difficulty.Hard;
                                break;
                            case (int)Difficulty.Hard:
                                A.Level = (double)Difficulty.Insane;
                                break;
                            }
                        break;
                    case Key.Down:
                        switch ((int)A.Level)
                        {
                            case (int)Difficulty.Normal:
                                A.Level = (double)Difficulty.Easy;
                                break;
                            case (int)Difficulty.Hard:
                                A.Level = (double)Difficulty.Normal;
                                break;
                            case (int)Difficulty.Insane:
                                A.Level = (double)Difficulty.Hard;
                                break;
                        }
                        break;
                }
            switch (e.Key) {
                case Key.A: A.Left  = true; break;
                case Key.D: A.Right = true; break;
                case Key.S: A.Down  = true; break;
                case Key.W: A.Up    = true; break;
            }
        }
        private void OnButtonKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.A: A.Left  = false; break;
                case Key.D: A.Right = false; break;
                case Key.S: A.Down  = false; break;
                case Key.W: A.Up    = false; break;
            }
        }
    }
}


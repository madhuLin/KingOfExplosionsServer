﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using Newtonsoft.Json;
using System.IO;

namespace KingOfExplosionsServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private string path = System.Environment.CurrentDirectory, terrain;
        TcpListener Server;             //伺服端網路監聽器(相當於電話總機)
        Socket Client;                  //給客戶用的連線物件(相當於電話分機)
        Thread Th_Svr;                  //伺服器監聽用執行緒(電話總機開放中)
        Thread Th_Clt;                  //客戶用的通話執行緒(電話分機連線中)
        Hashtable HT = new Hashtable(); //客戶名稱與通訊物件的集合(雜湊表)(key:Name, Socket)
        //Dictionary<string, List<string>> hashMap = new Dictionary<string, List<string>>();
        Dictionary<string, int> map = new Dictionary<string, int>();
        Dictionary<int, int> Protect = new Dictionary<int, int>();
        Tool toolNumber = new Tool();
        int [] heart = { 3,3,3,3};
        const int baseL = 50, N = 10;
        int[,] arr = new int[N,N];
        //Box[,] arrBox = new Box[N, N];
        int[,] arrProp = new int[N, N];
        List<int> dies = new List<int>();
        Tuple<int, int> [] playPosition = new Tuple<int, int>[4];
        int power = 1;
        private void button1_Click(object sender, EventArgs e)
        {
            //忽略跨執行緒處理的錯誤(允許跨執行緒存取變數)
            CheckForIllegalCrossThreadCalls = false;
            Th_Svr = new Thread(ServerSub);     //宣告監聽執行緒(副程式ServerSub)
            Th_Svr.IsBackground = true;         //設定為背景執行緒
            Th_Svr.Start();                     //啟動監聽執行緒
            button1.Enabled = false;            //讓按鍵無法使用(不能重複啟動伺服器) 
        }

        private void startServer()
        {
            //忽略跨執行緒處理的錯誤(允許跨執行緒存取變數)
            CheckForIllegalCrossThreadCalls = false;
            Th_Svr = new Thread(ServerSub);     //宣告監聽執行緒(副程式ServerSub)
            Th_Svr.IsBackground = true;         //設定為背景執行緒
            Th_Svr.Start();                     //啟動監聽執行緒
            button1.Enabled = false;            //讓按鍵無法使用(不能重複啟動伺服器) 
        }

        //接受客戶連線要求的程式(如同電話總機)，針對每一客戶會建立一個連線，以及獨立執行緒
        private void ServerSub()
        {
            //Server IP 和 Port
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            Server = new TcpListener(EP);       //建立伺服端監聽器(總機)
            Server.Start(100);                  //啟動監聽設定允許最多連線數100人
            while (true)                        //無限迴圈監聽連線要求
            {
                Client = Server.AcceptSocket(); //建立此客戶的連線物件Client
                Th_Clt = new Thread(Listen);    //建立監聽這個客戶連線的獨立執行緒
                Th_Clt.IsBackground = true;     //設定為背景執行緒
                Th_Clt.Start();                 //開始執行緒的運作
            }
        }
        //監聽客戶訊息的程式
        private void Listen()
        {
            Socket Sck = Client; //複製Client通訊物件到個別客戶專用物件Sck
            Thread Th = Th_Clt;  //複製執行緒Th_Clt到區域變數Th
            while (true)         //持續監聽客戶傳來的訊息
            {
                try                //用 Sck 來接收此客戶訊息，inLen 是接收訊息的 byte 數目
                {
                    byte[] B = new byte[1023];   //建立接收資料用的陣列，長度須大於可能的訊息
                    int inLen = Sck.Receive(B);  //接收網路資訊(byte陣列)
                    string Msg = Encoding.Default.GetString(B, 0, inLen); //翻譯實際訊息(長度inLen)
                    listBox2.Items.Add(Msg);
                    string[] datas = Msg.Split('|');
                    foreach (string tmpstr in datas)
                    {
                        if (tmpstr == "") continue;
                        Msg = tmpstr;
                        string Cmd = Msg.Substring(0, 1);                     //取出命令碼 (第一個字)
                        string Str = Msg.Substring(1);                        //取出命令碼之後的訊息

                        switch (Cmd)                                          //依據命令碼執行功能
                        {
                            case "0":  //有新使用者上線：新增使用者到名單中
                                if (listBox1.Items.IndexOf(Str) >= 0)
                                {
                                    listBox2.Items.Add("重複:" + Str);
                                    //byte[] R = Encoding.Default.GetBytes("R" + "使用者名稱重複");
                                    //Sck.Send(R, 0, R.Length, SocketFlags.None);
                                    Th.Abort();
                                    break;
                                }
                                HT.Add(Str, Sck);        //連線加入雜湊表，Key:使用者，Value:連線物件(Socket)
                                listBox1.Items.Add(Str); //加入上線者名單
                                SendTo("T"+ terrain, Str);
                                //SendAll(OnlineList());   //將目前上線人名單回傳剛剛登入的人(包含他自己) 
                                break;

                            case "9":
                                string[] Offline = Str.Split('|');
                                HT.Remove(Offline[0]);             //移除使用者名稱為Name的連線物件
                                listBox1.Items.Remove(Offline[0]); //自上線者名單移除Name
                                SendAll(OnlineList());      //將目前上線人名單回傳剛剛登入的人(不包含他自己) 
                                //if (hashMap.ContainsKey(Offline[1]))
                                //{
                                //    hashMap[Offline[1]].Remove(Offline[0]);
                                //    string allUser1 = "";
                                //    foreach (string user in hashMap[Offline[1]])
                                //    {
                                //        allUser1 += user + " ";
                                //    }
                                //    SendRoom("C" + allUser1, Offline[1]);
                                //}

                                Th.Abort();                 //結束此客戶的監聽執行緒
                                break;
                            case "1":                       //使用者傳送訊息給所有人
                                SendAll(Msg);               //廣播訊息
                                break;

                            case "J":
                                DataGame data = JsonConvert.DeserializeObject<DataGame>(Str);
                                GameAction(data, Str);
                                break;
                            default:                        //使用者傳送私密訊息

                                //string[] C = Str.Split('|');//切開訊息與收件者
                                //SendTo(Cmd + C[0], C[1]);   //C[0]是訊息，C[1]是收件者
                                break;
                        }
                    }
                    
                }
                catch (Exception)
                {
                    //有錯誤時忽略，通常是客戶端無預警強制關閉程式，測試階段常發生
                }
            }
        }


        private void GameAction(DataGame data, string json) 
        {
            switch (data.Action) 
            {
                case "DROP":
                    data.numberBomb = toolNumber.getNumber();
                    //data.Carry = power;
                    json = JsonConvert.SerializeObject(data);
                    Bomb bomb = new Bomb(data.Position.Item1, data.Position.Item2, data.numberBomb, data.UserNumber, power);
                    SendAll("J" + json);
                    bomb.reciprocal(25);
                    bomb.Exploded += CheckBom;
                    break;
                case "MOVE":
                    //string json = JsonConvert.SerializeObject(data);
                    if (canWalk(data)) {
                        listBox2.Items.Add("MOVE: " + json);
                        SendAll("J" + json);
                    }
                    
                    break;

            }
            
        }

        int[] Probability = { 0 , 8,8,8,8,8 };
        int getProp() 
        {
            Random random = new Random();
            int num = random.Next(Probability.Length);
            return Probability[num];
        }

        //處理炸彈爆炸
        private void CheckBom(object sender, EventArgs e)
        {
            Bomb bomb = (Bomb)sender;
            int x = bomb.X, y = bomb.Y;
            x = (x + 20) / 50;
            y = (y + 20) / 50;
            List<DataBomb> list = new List<DataBomb>();
            
            //炸彈一般情況
            int[] dir = new int[] { 0, 0, 1, 0, -1, 0 };
            for (int d = 0; d < dir.Length - 1; d++)
            {
                int r = y + dir[d], c = x + dir[d + 1];
                if (r < 0 || c < 0 || r >= N || c >= N) continue;
                if (arr[r, c] == 3) //檢查是否炸到箱子
                {
                    int type = getProp();
                    arr[r, c] = type;
                    DataBomb data = new DataBomb("BOMB", new Tuple<int, int>(r, c), type);
                    list.Add(data);
                }
                for (int i = 0; i < playPosition.Length; i++)  //檢查是否炸到玩家
                {
                    if (playPosition[i] == null || dies.Contains(i + 1) || (Protect.ContainsKey(bomb.userNumber) && Protect[bomb.userNumber] == 1)) continue;
                    var value = playPosition[i];
                    if (value.Item1 == r && value.Item2 == c)  //炸到玩家
                    {
                        Attack attack = new Attack("ACTTACK", i + 1, heart[i]--);
                        string attackStr = JsonConvert.SerializeObject(attack);
                        SendAll("H" + attackStr);
                        if (heart[i] <= 0) 
                        {
                            dies.Add(i + 1);
                            SendTo("O" + attackStr, (i + 1).ToString());
                        }

                    }
                }
            }
            
            //長度翻倍
            if(bomb.pow == 2)
            {
                int[] dir2 = new int[] { 0, 1, 0, -1, 0 };
                for (int d = 0; d < dir2.Length - 1; d++)
                {
                    int r = y + dir2[d] * 2, c = x + dir2[d + 1] * 2;
                    if (r < 0 || c < 0 || r >= N || c >= N) continue;
                    if (arr[r, c] == 3)
                    {
                        int type = getProp();
                        arr[r, c] = type;
                        DataBomb data = new DataBomb("BOMB", new Tuple<int, int>(r, c), type);
                        list.Add(data);
                    }
                    for (int i = 0; i < playPosition.Length; i++)
                    {
                        if (playPosition[i] == null || dies.Contains(i + 1) || (Protect.ContainsKey(bomb.userNumber) && Protect[bomb.userNumber] == 1)) continue;
                        var value = playPosition[i];
                        if (value.Item1 == r && value.Item2 == c)
                        {
                            Attack attack = new Attack("ACTTACK", i + 1, heart[i]--);
                            string attackStr = JsonConvert.SerializeObject(attack);
                            SendAll("H" + attackStr);
                            if (heart[i] <= 0)
                            {
                                dies.Add(i + 1);
                                SendTo("O" + attackStr, (i + 1).ToString());
                            }

                        }
                    }
                }
            }
            string json = JsonConvert.SerializeObject(list);
            SendAll("D" + bomb.numberBomb.ToString() + " " + json);
            if (dies.Count >= HT.Count - 1)
            {
                string dirStr = "";
                foreach (var die in dies) dirStr += die.ToString() + " ";
                foreach(string user in HT.Keys)
                {
                    int userNumber = int.Parse(user);
                    if (!dies.Contains(userNumber))
                    {
                        dirStr += user;
                        break;
                    }
                }
                SendAll("K" + dirStr);
            }

        }

        //處理行走事件
        private bool canWalk(DataGame data)  
        {
            int x = data.Position.Item1, y = data.Position.Item2;
            int basePL = baseL-10;
            for (int i = y / baseL; i < Math.Min(y / baseL + 2, N); i++)
            {
                for (int j = x / baseL; j < Math.Min(x / baseL + 2, N); j++)
                {
                    if (arr[i,j] == 1 || arr[i,j] == 2 || arr[i, j] == 3)
                    {
                        int xW = j * baseL, yW = i * baseL;
                        if (x > xW && x < xW + baseL && y > yW && y < yW + baseL) return false;
                        if (x > xW && x < xW + baseL && y + basePL > yW && y + basePL < yW + baseL) return false;
                        if (x + basePL > xW && x + basePL < xW + baseL && y > yW && y < yW + baseL) return false;
                        if (x + basePL > xW && x + basePL < xW + baseL && y + basePL > yW && y + basePL < yW + baseL) return false;
                    }
                }
            }
            int r = (y + 20) / baseL, c = (x + 20) / baseL;
            if (arr[r,c] >= 5)  //如果遇到道具
            {
                int type = arr[r, c];
                data.TypeProp = type;
                data.Action = "PORPSOVER";
                string jsonS = JsonConvert.SerializeObject(data);
                Tool tool = new Tool(jsonS);
                tool.Exploded += SendProp;  //結束觸發
                
                switch (type)
                {
                    case 5: //加速
                        tool.reciprocal(80);
                        break;
                    case 6: // 保護
                        Protect[data.UserNumber] = 1;
                        tool.reciprocal(60);
                        break;
                    case 7: //香蕉
                        tool.reciprocal(15);
                        break;
                    case 8: //加攻
                        power = 2;
                        tool.reciprocal(50);
                        break;
                    case 9: //回血
                        if (heart[data.UserNumber - 1] >= 3) heart[data.UserNumber - 1]--;
                        data.Carry = ++heart[data.UserNumber - 1];
                        break;

                }
                arr[r,c] = 0;
                data.Action = "PROP";
                data.Position = new Tuple<int, int>(r, c);
                string json = JsonConvert.SerializeObject(data);
                //listBox2.Items.Add("PORPSOVER: "+ json);
                SendAll("J" +  json);
            }
            playPosition[data.UserNumber-1] = new Tuple<int, int>(r, c);
            return true;
        }


        //建立線上名單
        private string OnlineList()
        {
            string L = "L";             //代表線上名單的命令碼(字頭)
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                L += listBox1.Items[i]; //逐一將成員名單加入L字串
                //不是最後一個成員要加上","區隔
                if (i < listBox1.Items.Count - 1)
                {
                    L += ",";
                }
            }
            return L;
        }
        //傳送訊息給指定的客戶
        private void SendProp(object sender, EventArgs e)
        {
            Tool tool = (Tool)sender;
            DataGame data = JsonConvert.DeserializeObject<DataGame>(tool.str);
            if (data.TypeProp == 6) Protect[data.UserNumber] = 0;
            else if (data.TypeProp == 8) power = 1;
            byte[] B = Encoding.Default.GetBytes("J" + tool.str+ "|");  //訊息轉譯為byte陣列
            Socket Sck = (Socket)HT[data.UserNumber.ToString()];              //取出發送對象User的通訊物件
            Sck.Send(B, 0, B.Length, SocketFlags.None); //發送訊息
        }
        //傳送訊息給指定的客戶
        private void SendTo(string Str, string User)
        {
            byte[] B = Encoding.Default.GetBytes(Str + "|");  //訊息轉譯為byte陣列
            Socket Sck = (Socket)HT[User];              //取出發送對象User的通訊物件
            Sck.Send(B, 0, B.Length, SocketFlags.None); //發送訊息
        }
        //傳送訊息給所有的線上客戶
        private void SendRoom(string Str, string user)
        {
            byte[] B = Encoding.Default.GetBytes(Str+ "|");   //訊息轉譯為Byte陣列
            foreach (Socket s in HT.Values)              //HT雜湊表內所有的Socket
            {
                if (user != HT.Keys.ToString())
                    s.Send(B, 0, B.Length, SocketFlags.None);//傳送資料
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            path = path.Substring(0, path.IndexOf("bin"));
            if (File.Exists(path + "Terrain.txt"))
            {
                using (StreamReader stringReader = new StreamReader(path + "Terrain.txt"))
                {
                    terrain = stringReader.ReadToEnd();
                }
                listBox2.Items.Add(terrain);
                using (StringReader sr = new StringReader(terrain))
                {
                    int i = 0;
                    string line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        for (int j = 0; j < line.Length; j++) arr[i,j] = line[j] - '0';
                        i++;
                    }

                }
            }
            textBox1.Text = MyIP();
            startServer();
        }

        private string MyIP()
        {
            string hn = Dns.GetHostName();
            IPAddress[] ip = Dns.GetHostEntry(hn).AddressList;
            foreach (IPAddress ipAddr in ip)
            {
                if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
                { return ipAddr.ToString(); }
            }
            return "";
        }

        //傳送訊息給所有的線上客戶
        private void SendAll(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str + "|");   //訊息轉譯為Byte陣列
            foreach (Socket s in HT.Values)              //HT雜湊表內所有的Socket
            {
                s.Send(B, 0, B.Length, SocketFlags.None);//傳送資料
            }
                
        }

    }
}

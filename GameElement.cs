﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KingOfExplosionsServer
{
    public class Tool
    {
        int capacity = 100;
        int i = 0;
        public int getNumber() 
        {
            i++;
            if (i >= 100) i %= capacity;
            return i;
        }

        private int V;
        private System.Threading.Timer timer;
        private object lockObject = new object();
        // 定义倒计时结束时触发的事件
        public event EventHandler Exploded;
        public Tool() { }
        public string str { get; set; }
        public Tool(string str)
        {
            this.str = str;
        }

        public void reciprocal(int t)
        {
            V = t;
            timer = new System.Threading.Timer(CountDown, null, 0, 100);
        }

        private void CountDown(object state)
        {
            lock (lockObject)
            {
                if (V == 0)
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    OnExploded();

                }
                else if (V > 0)
                {
                    V -= 1;
                }

            }
        }
        protected virtual void OnExploded()
        {
            Exploded?.Invoke(this, EventArgs.Empty);
        }
    }
    public class GameElement
    {
        public int X { get; set; }
        public int Y { get; set; }

        public int W { get; set; }
        public int H { get; set; }

        protected string path = System.Environment.CurrentDirectory;
    }
    public class Bomb : GameElement
    {
        private int V;
        private System.Threading.Timer timer;
        private object lockObject = new object();
        // 定义倒计时结束时触发的事件
        public event EventHandler Exploded;
        public int numberBomb { get; set; }
        public int userNumber { get; set; }
        public Bomb(int x, int y, int number, int userNumber)
        {
            X = x;
            Y = y;
            numberBomb = number;
            this.userNumber = userNumber;
        }
        public void reciprocal(int t)
        {
            V = t;
            timer = new System.Threading.Timer(CountDown, null, 0, 100);
        }

        private void CountDown(object state)
        {
            lock (lockObject)
            {
                if (V == 0)
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    OnExploded();

                }
                else if (V > 0)
                {
                    V -= 1;
                }

            }
        }
        protected virtual void OnExploded()
        {
            Exploded?.Invoke(this, EventArgs.Empty);
        }
    }
}

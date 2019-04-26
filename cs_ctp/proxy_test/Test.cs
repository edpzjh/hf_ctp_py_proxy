﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HaiFeng
{
    public class TestQuote
    {
        CTPQuote _q = null;
        string _investor, _pwd, _inst;
        public string QuoteAddr = "tcp://180.168.146.187:13040", Broker = "9999";

        public TestQuote(string investor, string pwd, string instrument)
        {
            _investor = investor;
            _pwd = pwd;
            _inst = instrument;

            _q = new CTPQuote();

            _q.OnFrontConnected += _q_OnFrontConnected;
            _q.OnRspUserLogin += _q_OnRspUserLogin;
            _q.OnRspUserLogout += _q_OnRspUserLogout;
            _q.OnRtnTick += _q_OnRtnTick;
            _q.OnRtnError += _q_OnRtnError;
        }

        public void Release()
        {
            _q.ReqUserLogout();
        }

        public void Run()
        {
            _q.ReqConnect(QuoteAddr);
        }

        void Log(string pMsg)
        {
            Console.WriteLine(DateTime.Now.TimeOfDay + "\t" + pMsg);
        }

        private void _q_OnFrontConnected(object sender, EventArgs e)
        {
            Log("connected");
            _q.ReqUserLogin(_investor, _pwd, this.Broker);
        }

        private void _q_OnRspUserLogin(object sender, IntEventArgs e)
        {
            if (e.Value == 0)
            {
                Log($"登录成功:{_investor}");
                _q.ReqSubscribeMarketData(_inst);
            }
            else
            {
                //_q.OnFrontConnected -= _q_OnFrontConnected;    //解决登录错误后不断重连导致再不断登录的错误
                Log($"登录错误：{e.Value}");
                _q.ReqUserLogout();
            }
        }

        private void _q_OnRtnTick(object sender, TickEventArgs e)
        {
            Log($"{e.Tick.InstrumentID}\t{e.Tick.LastPrice}");
        }

        private void _q_OnRspUserLogout(object sender, IntEventArgs e)
        {
            Log($"quote logout: {e.Value}");
        }

        private void _q_OnRtnError(object sender, ErrorEventArgs e)
        {
            Log(e.ErrorMsg);
        }
    }

    class TestTrade
    {
        CTPTrade _t = null;
        string _inst;
        double _price;
        public string TradeAddr, Broker, AppID, AuthCode, ProcInfo, Investor, Pwd;


        public TestTrade(string pInstrument, double pPrice)
        {
            _inst = pInstrument;
            _price = pPrice;
            _t = new CTPTrade();
        }

        public void Release()
        {
            _t.ReqUserLogout();
        }

        void Log(string pMsg)
        {
            Console.WriteLine(DateTime.Now.TimeOfDay + "\t" + pMsg);
        }

        public void Run()
        {
            _t.OnFrontConnected += _t_OnFrontConnected;
            _t.OnRspUserLogout += _t_OnRspUserLogout;
            _t.OnRspUserLogin += _t_OnRspUserLogin;
            _t.OnRspAuth += _t_OnRspAuth;

            _t.OnRtnOrder += _t_OnRtnOrder;
            _t.OnRtnTrade += _t_OnRtnTrade;
            _t.OnRtnCancel += _t_OnRtnCancel;
            _t.OnRtnNotice += _t_OnRtnNotice;
            Console.WriteLine(_t.Version);
            _t.Investor = Investor;
            _t.Broker = Broker;
            _t.ReqConnect(TradeAddr);
            //_t.ReqConnect("tcp://218.202.237.33:10002");
            //_t.ReqConnect("tcp://172.20.28.57:41205");
        }
        private void _t_OnFrontConnected(object sender, EventArgs e)
        {
            _t.ReqAuth(ProcInfo, AuthCode, AppID);
        }

        private void _t_OnRspAuth(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"认证：{e.ErrorMsg}");
            _t.ReqUserLogin(Investor, Pwd, Broker);
        }


        private void _t_OnRspUserLogin(object sender, ErrorEventArgs e)
        {
            if (e.ErrorID == 0)
            {
                Log("登录成功");
                foreach (var v in _t.DicPositionField.Values)
                {
                    Log($"posi:{v.InstrumentID}\t{v.Direction}\t{v.Price}\t{v.Position}");
                }
                //            new Thread(() =>
                //{
                //	// 需要另启线程,在onrsp中处理,会导致线程被阻塞,后续的查询无法返回.
                //	Thread.Sleep(3000);
                //	foreach (var v in _t.DicPositionField.Values)
                //	{
                //		Log($"posi:{v.InstrumentID}\t{v.Direction}\t{v.Price}\t{v.Position}");
                //	}
                //}).Start();
                //_t.ReqOrderInsert(_inst, DirectionType.Buy, OffsetType.Open, _price, 1, 1000);
            }
            else
            {
                Log($"登录错误：{e.ErrorID}={e.ErrorMsg}");
            }
        }

        private void _t_OnRtnNotice(object sender, StringEventArgs e)
        {
            Console.WriteLine(e.Value);
        }

        private void _t_OnRtnCancel(object sender, OrderArgs e)
        {
            Log($"cancel:{e.Value.StatusMsg}\t{e.Value.InstrumentID}\t{e.Value.Direction}\t{e.Value.Offset}\t{e.Value.LimitPrice}\t{e.Value.Volume}\t{e.Value.StatusMsg}");
        }

        private void _t_OnRtnTrade(object sender, TradeArgs e)
        {
            Log($"trade:{e.Value.InstrumentID}\t{e.Value.Direction}\t{e.Value.Offset}\t{e.Value.Price}\t{e.Value.Volume}");
            foreach (var v in _t.DicPositionField.Values)
            {
                Log($"posi:{v.InstrumentID}\t{v.Direction}\t{v.Price}\t{v.Position}");
            }
        }

        private void _t_OnRtnOrder(object sender, OrderArgs e)
        {
            Log($"order:{e.Value.InstrumentID}\t{e.Value.Direction}\t{e.Value.Offset}\t{e.Value.LimitPrice}\t{e.Value.Volume}");

            if (e.Value.IsLocal)
                _t.ReqOrderAction(e.Value.OrderID);
        }


        private void OnRtnInstrumentStatus(ref CThostFtdcInstrumentStatusField pInstrumentStatus)
        {
            Log($"{pInstrumentStatus.InstrumentID}:{pInstrumentStatus.InstrumentStatus}");
        }

        private void _t_OnRspUserLogout(object sender, IntEventArgs e)
        {
            Log("t: logout:" + e.Value);
        }
    }

}

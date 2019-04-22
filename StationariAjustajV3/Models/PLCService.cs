using S7.Net;
using StationariAjustajV3.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace StationariAjustajV3.Models
{
    public enum ConnectionStates
    {
        Offline,
        Connecting,
        Online
    }

    class PLCService
    {
        // Variabile logica
        private readonly Plc _client;
        private volatile object _locker = new object();
        // Adrese variabile Plc
        private readonly CpuType _cpuType;
        private readonly string _ip;
        private readonly short _rack;
        private readonly short _slot;
        private readonly string _nume;
        // Adrese variabile de citit (predefinite pentru stationari ajustaj)
        private readonly string _oprire1;
        private readonly string _oprire2;
        private readonly string _defMecanic;
        private readonly string _defElectric;
        private readonly string _oprireProgramata;
        private readonly string _oprireTehnologica;
        private readonly string _lipsaPodMaterial;
        

        public PLCService(CpuType cpuType, string ip, short rack, short slot, string nume, string oprire1, string oprire2, string defMecanic, 
            string defElectric, string oprireProgramata, string oprireTehnologica, string lipsaPodMaterial)
        {
            // Initializare adrese variabile PLC
            _client = new Plc(cpuType, ip, rack, slot);
            _cpuType = cpuType;
            _ip = ip;
            _rack = rack;
            _slot = slot;
            _nume = nume;
            _oprire1 = oprire1;
            _oprire2 = oprire2;
            _defMecanic = defMecanic;
            _defElectric = defElectric;
            _oprireProgramata = oprireProgramata;
            _oprireTehnologica = oprireTehnologica;
            _lipsaPodMaterial = lipsaPodMaterial;

            // Iniatializare cu 0 proprietati Plc
            NumePlc = nume;
            Oprire1 = false;
            Oprire2 = false;
            DefMecanic = false;
            DefElectric = false;
            OprireProgramata = false;
            OprireTehnologica = false;
            LipsaPodMaterial = false;
            MotivStationare = "";
        }

        public ConnectionStates ConnectionState { get; private set; }

        public string NumePlc { get; private set; }

        public bool Oprire1 { get; private set; }

        public bool Oprire2 { get; private set; }

        public bool DefMecanic { get; private set; }

        public bool DefElectric{ get; private set; }

        public bool OprireProgramata { get; private set; }

        public bool OprireTehnologica { get; private set; }

        public bool LipsaPodMaterial { get; private set; }

        public TimeSpan ScanTime { get; private set; }

        public string MotivStationare { get; private set; }


        public void Connect()
        {
            if (ConnectionState != ConnectionStates.Online)
            {
                try
                {
                    ConnectionState = ConnectionStates.Connecting;
                    var performTaskCheckAvailability = Task.Run(() =>
                    {
                        if (_client.IsAvailable)
                            _client.Open();
                    });
                    performTaskCheckAvailability.Wait(TimeSpan.FromSeconds(0.5)); // Asteapta Task sa fie complet in 1 sec 
                    

                    bool result = false;
                    var performTaskCitireVariabile = Task.Run(() =>
                    {
                        result = _client.IsConnected;
                    });
                    performTaskCitireVariabile.Wait(TimeSpan.FromSeconds(1)); // Asteapta Task sa fie complet in 1 sec 

                    if (result)
                    {
                        ConnectionState = ConnectionStates.Online;
                        LogMessegeRaportareAjustaj.WriteMessegeToLogFile("S-a conectat PLC " + _nume);
                    }
                    else
                    {
                        Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: nu a reusit conectarea");
                        ConnectionState = ConnectionStates.Offline;
                    }

                    //MessageBox.Show("S-a conectat PLC " +_nume);
                }
                catch (PlcException err)
                {
                    ConnectionState = ConnectionStates.Offline;
                    LogMessegeRaportareAjustaj.WriteMessegeToLogFile(err.ErrorCode.ToString());
                    LogMessegeRaportareAjustaj.WriteMessegeToLogFile("eroare PlcException " +
                        "la conectare Plc" + _nume);
                }
                catch (Exception ex)
                {
                    ConnectionState = ConnectionStates.Offline;
                    LogMessegeRaportareAjustaj.WriteMessegeToLogFile(ex.Message.ToString());
                    LogMessegeRaportareAjustaj.WriteMessegeToLogFile("eroare conectare Plc: " +
                        _nume + " dupa catch eroare PlcException");
                    //throw;
                }
                
            }
            
        }

        public void Disconnect()
        {
            if (_client.IsConnected)
            {
                _client.Close();
                ConnectionState = ConnectionStates.Offline;

                LogMessegeRaportareAjustaj.WriteMessegeToLogFile("S-a executat deconectare PLC" + _nume);
                //MessageBox.Show("S-a deconectat PLC" + _nume);
            }
        }
        
        public void RefreshValues()
        {
            DateTime startScanTime = DateTime.Now;
            if (ConnectionState == ConnectionStates.Online)
            {
                lock (_locker)
                {
                    var performTaskCitireVariabile = Task.Run(() =>
                    {
                        NumePlc = _nume;
                        Oprire1 = Convert.ToBoolean(_client.Read(_oprire1)); // BreakDown in Progress [1 = BreakDown]
                        Oprire2 = Convert.ToBoolean(_client.Read(_oprire2));// Second Break Down In Progress [1 = Second Break Down]
                        DefMecanic = Convert.ToBoolean(_client.Read(_defMecanic)); // Mechanical Cause
                        DefElectric = Convert.ToBoolean(_client.Read(_defElectric)); // Electrical Cause
                        OprireProgramata = Convert.ToBoolean(_client.Read(_oprireProgramata)); // Programmed Cause
                        OprireTehnologica = Convert.ToBoolean(_client.Read(_oprireTehnologica)); // Technological Cause
                        LipsaPodMaterial = Convert.ToBoolean(_client.Read(_lipsaPodMaterial)); // No Creane/ No Material Cause  

                        if (Oprire1)
                        {
                            if (DefMecanic)
                                MotivStationare = "Defect mecanic";
                            else if (DefElectric)
                                MotivStationare = "Defect electric";
                            else if (OprireProgramata)
                                MotivStationare = "Oprire programata";
                            else if (OprireTehnologica)
                                MotivStationare = "Oprire tehnologica";
                            else if (LipsaPodMaterial)
                                MotivStationare = "Lipsa pod rulant / Lipsa material";
                            else MotivStationare = "Nu s-a apasat cauza";
                        }
                        else MotivStationare = "Functioneaza";

                    });
                    performTaskCitireVariabile.Wait(TimeSpan.FromSeconds(2)); // Asteapta Task sa fie complet in 2 sec  
                }
            }
            ScanTime = DateTime.Now - startScanTime;
            
            // Verificam interval citire variabile daca este mai mare de 900ms, si mai incercam sa reconectam daca is revine
            if (ScanTime.TotalMilliseconds > 1900)
            {
                ConnectionState = ConnectionStates.Offline;
                Disconnect();
                MotivStationare = "Plc deconectat";
            }

            // Verificam daca este offline, incercam sa ne reconectam
            if (ConnectionState == ConnectionStates.Offline)
            {
                if (DateTime.Now.ToString("ss") == "20")
                    Connect();
            }
            //MessageBox.Show("Oprire 1: "+ Oprire1 +
            //    "\nOprire 2: " + Oprire2 +
            //    "\nDefect Mcanic: " + DefMecanic +
            //    "\nDefect Electric: " + DefElectric +
            //    "\nOprire programata: " + OprireProgramata +
            //    "\nOprire tehnologica: " + OprireTehnologica +
            //    "\nLipsa pod/ material: " + LipsaPodMaterial);
        }

    }
}

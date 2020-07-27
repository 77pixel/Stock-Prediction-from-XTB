using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using xAPI.Sync;
using xAPI.Responses;
using xAPI.Commands;
using xAPI.Records;
using xAPI.Codes;
using System.IO;
using System;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace stockANN
{
    public partial class MainWindow : Window
    {
        Line pozLine = new Line();
        TextBlock pozText = new TextBlock();
        NeuralNetwork nn;
        InstrumentKarta instrumentKarta;
        private delegate void NoArgDelegate();

        static Server serverData = Servers.REAL;
        static string userId = "XTB_ID";
        static string password = "HASLO";

        public MainWindow()
        {
            InitializeComponent();
            ListaTransakcji();
            //TransakcjaStart("OIL", 0.01, TRADE_OPERATION_CODE.BUY);
            ListaInstrumentow();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
            dispatcherTimer.Start();

        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            ListaTransakcji();
        }
        private void ListaTransakcji()
        {
            try
            {
                SyncAPIConnector connector = new SyncAPIConnector(serverData);
                Credentials credentials = new Credentials(userId, password, "", "pixelNet");
                LoginResponse loginResponse = APICommandFactory.ExecuteLoginCommand(connector, credentials, true);
                TradesResponse tradesResponse = APICommandFactory.ExecuteTradesCommand(connector, true);

                listaTran.Items.Clear();
                foreach (xAPI.Records.TradeRecord rec in tradesResponse.TradeRecords)
                {
                    string rodz = "";
                    if (rec.Cmd == 1) rodz = "Sell"; else rodz = "Buy";
                    listaTran.Items.Add(new PozTran()
                    {
                        Nazwa = rec.Symbol,
                        CenaZak = (double)rec.Open_price,
                        Zysk = (double)rec.Profit,
                        Rodzaj = rodz
                    }); ;

                    //if (rec.Profit > 30) TransakcjaStop(rec);
                    LogoutResponse logoutResponse = APICommandFactory.ExecuteLogoutCommand(connector);
                }

                tradesResponse = null;
            } catch { }
        }
        private void ListaInstrumentow()
        {
            
            try
            {
                SyncAPIConnector connector = new SyncAPIConnector(serverData);
                Credentials credentials = new Credentials(userId, password, "", "pixelNet");
                LoginResponse loginResponse = APICommandFactory.ExecuteLoginCommand(connector, credentials, true);
                AllSymbolsResponse allSymbolsResponse = APICommandFactory.ExecuteAllSymbolsCommand(connector, true);

                ListaInstrumentow fx = new ListaInstrumentow() { Symbol = "FX" };
                ListaInstrumentow cmd = new ListaInstrumentow() { Symbol = "CMD" };
                ListaInstrumentow crt = new ListaInstrumentow() { Symbol = "CRT" };
                

                foreach (SymbolRecord symbolRecord in allSymbolsResponse.SymbolRecords)
                {
                    if(symbolRecord.CategoryName == "FX")
                    {
                        fx.Pozycje.Add(symbolRecord);
                    }
                    if (symbolRecord.CategoryName == "CMD")
                    {
                        cmd.Pozycje.Add(symbolRecord);
                    }
                    if (symbolRecord.CategoryName == "CRT")
                    {
                        crt.Pozycje.Add(symbolRecord);
                    }
                    
                }

                treev.Items.Add(fx); 
                treev.Items.Add(cmd); 
                treev.Items.Add(crt);

                LogoutResponse logoutResponse = APICommandFactory.ExecuteLogoutCommand(connector);


            }
            catch (Exception e){ MessageBox.Show(e.Message); }

        }
        private void TransakcjaStart(string Nazwa, double volume, TRADE_OPERATION_CODE toc)
        {         
            try
            {
                SyncAPIConnector connector = new SyncAPIConnector(serverData);
                Credentials credentials = new Credentials(userId, password, "", "pixelNet");
                LoginResponse loginResponse = APICommandFactory.ExecuteLoginCommand(connector, credentials, true);
                SymbolResponse symbolResponse = APICommandFactory.ExecuteSymbolCommand(connector, Nazwa);
                double price = symbolResponse.Symbol.Ask.GetValueOrDefault();
                double sl = 0.0;
                double tp = 0.0;
                string symbol = symbolResponse.Symbol.Symbol;
                long order = 0;
                long expiration = 0;
                TradeTransInfoRecord ttOpenInfoRecord = new TradeTransInfoRecord(
                    toc,
                    TRADE_TRANSACTION_TYPE.ORDER_OPEN,
                    price, sl, tp, symbol, volume, order, "", expiration);
                TradeTransactionResponse tradeTransactionResponse = APICommandFactory.ExecuteTradeTransactionCommand(connector, ttOpenInfoRecord);
                LogoutResponse logoutResponse = APICommandFactory.ExecuteLogoutCommand(connector);

            }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }
        private void TransakcjaStop(TradeRecord tradeRecordStop)
        {
            try
            {
                SyncAPIConnector connector = new SyncAPIConnector(serverData);
                Credentials credentials = new Credentials(userId, password, "", "pixelNet");
                LoginResponse loginResponse = APICommandFactory.ExecuteLoginCommand(connector, credentials, true);
                TRADE_OPERATION_CODE toc;
                if (tradeRecordStop.Cmd == 1) toc = TRADE_OPERATION_CODE.SELL; else toc = TRADE_OPERATION_CODE.BUY;

                double price = tradeRecordStop.Close_price.GetValueOrDefault();
                double sl = 0.0;
                double tp = 0.0;
                string symbol = tradeRecordStop.Symbol;
                double? volume = tradeRecordStop.Volume;
                long order = tradeRecordStop.Order.GetValueOrDefault();
                long expiration = 0;
                TradeTransInfoRecord ttCloseInfoRecord = new TradeTransInfoRecord(
                    toc,
                    TRADE_TRANSACTION_TYPE.ORDER_CLOSE,
                    price, sl, tp, symbol, volume, order, "", expiration);
                TradeTransactionResponse closeTradeTransactionResponse = APICommandFactory.ExecuteTradeTransactionCommand(connector, ttCloseInfoRecord, true);
                LogoutResponse logoutResponse = APICommandFactory.ExecuteLogoutCommand(connector);

            }
            catch (Exception e){ MessageBox.Show(e.Message); }
        }
        private void PomyslSiec(double[] tabelaWyniki)
        {
            double[][] tabelaNauki = new double[99][];

            for (int ind = 0; ind < 99; ind++)
            {
                double[] linia = new double[200];
                for (int lind = 0; lind < 200; lind++)
                {
                    linia[lind] = tabelaWyniki[lind + (ind * 100)];
                }
                tabelaNauki[ind] = linia;
            }

            //trenowanie
            int ilEpok = 30;

            double wartNauki = 0.03;
            double momentum = 0.005;

            int iloscWE = 100;
            int ukryteW = 100;
            int iloscWY = 100;
            nn = new NeuralNetwork(iloscWE, ukryteW, iloscWY);

            double[] weights = nn.Train(tabelaNauki, ilEpok, wartNauki, momentum, tb_info);
            double trainAcc = nn.Accuracy(tabelaNauki);

            tb_info.AppendText("\nWyliczono " + weights.Length.ToString() + " wag");
            tb_info.AppendText("\nDokładność treningu = " + trainAcc.ToString("F4"));
            tb_info.AppendText("\n");

            nn.ZapiszPlikNN(ilEpok, wartNauki, momentum);

        }
        private void PomyslSiecIlosc(double[] tabelaWyniki, int lwej, int lwyj)
        {
            int liczbaBlok = 10000 / (lwej) - 1;
            double[][] tabelaNauki = new double[liczbaBlok][];
            
            for (int ind = 0; ind < liczbaBlok; ind++)
            {
                double[] linia = new double[lwej + lwyj];
               // if (lwej + lwyj + (ind * lwej) > liczbaBlok) break;
                
                for (int linwe = 0; linwe < (lwej+lwyj) ; linwe++)
                {
                        
                    linia[linwe] = tabelaWyniki[linwe + (ind * lwej)];
                }

                tabelaNauki[ind] = linia;
            }

            //trenowanie
            int ilEpok = 30;

            double wartNauki = 0.03;
            double momentum = 0.005;

            int iloscWE = lwej;
            int ukryteW = 100;
            int iloscWY = lwyj;
            nn = new NeuralNetwork(iloscWE, ukryteW, iloscWY);

            double[] weights = nn.Train(tabelaNauki, ilEpok, wartNauki, momentum, tb_info);
            double trainAcc = nn.Accuracy(tabelaNauki);

            tb_info.AppendText("\nWyliczono " + weights.Length.ToString() + " wag");
            tb_info.AppendText("\nDokładność treningu = " + trainAcc.ToString("F4"));
            tb_info.AppendText("\n");

            nn.ZapiszPlikNN(ilEpok, wartNauki, momentum);

        }
        private void RysujWykres(double[] dane, Canvas canv, Brush brush, int multi, PointCollection pZysk )
        {
            cWykresRazem.Children.Clear();
            //wykres danych
            int ilo = dane.Length;
            const double margin = 50;

            double dmin = 100000;
            double dmax = 0;
            
            for (int x = 0; x < ilo; x += 1)
            {
                dane[x] = dane[x] * instrumentKarta.podzielp;
                if (dane[x] > dmax) dmax = dane[x];
                if (dane[x] < dmin) dmin = dane[x];
            }
           
            string cyfr = instrumentKarta.ulamek.ToString("D");
            tb_dmax.Text = dmax.ToString("F0" + cyfr);
            tb_dmin.Text = dmin.ToString("F0" + cyfr);

            double cwys = canv.RenderSize.Height;
            double cszerMinuta = (canv.RenderSize.Width - margin) / ilo;
            double ypoz = cwys / (dmax - dmin);

            PointCollection points = new PointCollection();
            PointCollection pwylicz = new PointCollection();

            for (int x = 0; x < ilo; x++)
            {
                double yd = dane[x]; //offset wykresu
                yd -= dmin;
                yd = cwys - (ypoz * yd);
                int y = Convert.ToInt32(yd);
                double xpoz = cszerMinuta * x + margin;
                if (multi == 1) { if (x < slWej.Value) points.Add(new Point(xpoz, y)); else pwylicz.Add(new Point(xpoz, y)); }
                else points.Add(new Point(xpoz, y));

                // pionowe linie osi x
                Line myLine = new Line
                {
                    Stroke = Brushes.Black,
                    X1 = xpoz,
                    X2 = xpoz,
                    Y1 = 0,
                    Y2 = cwys,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };
                canv.Children.Add(myLine);
            }

            //wartosci na osi y
            for (int y = 0; y < cwys; y += 10)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.FontSize = 8;
                double wart = dmax - (((dmax - dmin) / cwys) * y);
                textBlock.Text = wart.ToString("F0" + instrumentKarta.ulamek.ToString("D"));
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                Canvas.SetLeft(textBlock, 20);
                Canvas.SetTop(textBlock, y);
                canv.Children.Add(textBlock);
            }

            if (multi == 1)
            {
                Polyline polyline = new Polyline
                {
                    StrokeThickness = 2,
                    Stroke = Brushes.Coral,
                    Points = points
                };

                Polyline polyline2 = new Polyline
                {
                    StrokeThickness = 2,
                    Stroke = Brushes.GreenYellow,
                    Points = pwylicz
                };
                canv.Children.Add(polyline);
                canv.Children.Add(polyline2);

                // pionowa granica czasu
                Line lgranica = new Line
                {
                    Stroke = Brushes.Blue,
                    X1 = cszerMinuta * slWej.Value + margin,
                    X2 = cszerMinuta * slWej.Value + margin,
                    Y1 = 0,
                    Y2 = cwys,
                    StrokeThickness = 2,
                    SnapsToDevicePixels = true
                };
                canv.Children.Add(lgranica);

                //przewidywany zysk
                PointCollection pTempZusk = new PointCollection();
                foreach (Point p in pZysk)
                {
                    double yd = p.Y;
                    yd -= dmin;
                    yd = cwys - (ypoz * yd);
                    int y = Convert.ToInt32(yd);
                    double xpoz = cszerMinuta * (p.X + slWej.Value) + margin;
                    pTempZusk.Add(new Point(xpoz, y));


                }
                Polyline lZysk = new Polyline
                {
                    StrokeThickness = 4,
                    Stroke = Brushes.Red,
                    Points = pTempZusk
                };
                canv.Children.Add(lZysk);

            }
            else
            {
                Polyline polyline = new Polyline
                {
                    StrokeThickness = 2,
                    Stroke = brush,
                    Points = points
                };
                canv.Children.Add(polyline);
            }
        }
        private void cWykresRazem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                cWykresRazem.Children.Remove(pozLine);
                cWykresRazem.Children.Remove(pozText);
                Point p = Mouse.GetPosition(cWykresRazem);
                if (p.X > 50 && p.Y > 0)
                {
                    // tekst info
                    double dmax = Convert.ToDouble(tb_dmax.Text);
                    double dmin = Convert.ToDouble(tb_dmin.Text);
                    double wart = dmax - (((dmax - dmin) / cWykresRazem.ActualHeight) * p.Y);
                    double czas = (p.X - 50) / ((cWykresRazem.ActualWidth - 50) / (slWej.Value + slWyj.Value)) - slWej.Value;
                    string cz = czas.ToString("+0;-#");
                    
                    pozText = new TextBlock();
                    pozText.FontSize = 10;
                    pozText.Text = wart.ToString("F0" + instrumentKarta.ulamek.ToString("D")); 
                    pozText.Text += " (" + cz + " min)";
                    pozText.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                    Canvas.SetLeft(pozText, p.X + 10);
                    Canvas.SetTop(pozText, p.Y - 25);
                    cWykresRazem.Children.Add(pozText);

                    //linia pozioma
                    pozLine = new Line
                    {
                        Stroke = Brushes.Black,
                        X1 = 50,
                        X2 = cWykresRazem.ActualWidth,
                        Y1 = p.Y,
                        Y2 = p.Y,
                        StrokeThickness = 1,
                        SnapsToDevicePixels = true,
                        Name = "poziom"
                    };
                    cWykresRazem.Children.Add(pozLine);
                }
            }
            catch (Exception ex){  }
        }
        public void SplashTxt(string txt)
        {
            cWykresRazem.Children.Clear();
            TextBlock tb = new TextBlock();
            tb.FontSize = 60;
            tb.Text = txt;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            Canvas.SetLeft(tb, 370);
            Canvas.SetTop(tb, 135);
            int test = 1;
            test = cWykresRazem.Children.Add(tb);
            cWykresRazem.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (NoArgDelegate)delegate { });
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tb_info.Clear();
            SplashTxt("..LICZENIE..");

            instrumentKarta.Notowania(PERIOD_CODE.PERIOD_M1, 17800);

            string cyfr = instrumentKarta.ulamek.ToString("D");
            tb_dzmax.Text = ((double)instrumentKarta.info.High).ToString("F0" + cyfr);
            tb_dzmin.Text = ((double)instrumentKarta.info.Low).ToString("F0" + cyfr);
            tb_bid.Text = ((double)instrumentKarta.info.Bid).ToString("F0" + cyfr);
            tb_ask.Text = ((double)instrumentKarta.info.Ask).ToString("F0" + cyfr);

            //PomyslSiec(instrumentKarta.tabelaWynikow);
            PomyslSiecIlosc(instrumentKarta.tabelaWynikow, (int)slWej.Value, (int)slWyj.Value);
            double[] real = instrumentKarta.danewykres((int)slWej.Value);
            double[] dane = nn.ComputeOutputs(real);
            double[] razem = new double[(int)slWej.Value + (int)slWyj.Value];

            double offset = real[(int)slWej.Value - 1] - dane[0];
            
            for(int i = 0; i < slWej.Value; i++)
            {
                razem[i] = real[i];  
            }

            double dmin = 100000;
            double dmax = 0;
            
            int dminPoz = 0;
            int dmaxPoz = 0;

            for (int i = 0; i < slWyj.Value; i++)
            {
                double policz = dane[i] + offset;

                razem[i + (int)slWej.Value] = policz;

                if (policz > dmax) { dmax = policz; dmaxPoz = i; }
                if (policz < dmin) { dmin = policz; dminPoz = i; }

            }

            double zysk = dmax - dmin;
            zysk = dmax - dmin;

            zysk *= (double)instrumentKarta.podzielp;
            zysk -= ((double)instrumentKarta.info.Bid - (double)instrumentKarta.info.Ask);
            zysk = Math.Round(zysk, 2);
            String opis = "Postaw na: ";
            PointCollection pTemp = new PointCollection();
            if (dmaxPoz > dminPoz)//rosnie
            {
                opis += "BUY za " + (dminPoz - 1).ToString() + " minut i po " + (dmaxPoz - dminPoz).ToString() + " minutach ( w " +dmaxPoz.ToString() + ") zyskaj: " + zysk.ToString("F") + " " + instrumentKarta.info.CurrencyProfit;
                pTemp.Add(new Point(dminPoz, dmin * (double)instrumentKarta.podzielp));
                pTemp.Add(new Point(dmaxPoz, dmax * (double)instrumentKarta.podzielp));
            }
            else //maleje 
            {
                opis += "SELL za " + (dmaxPoz - 1).ToString() + " minut i po " + (dminPoz - dmaxPoz).ToString() + " minutach ( w " + dminPoz.ToString() + ") zyskaj: " + zysk.ToString("F") + " " + instrumentKarta.info.CurrencyProfit;
                pTemp.Add(new Point(dmaxPoz, dmax * (double)instrumentKarta.podzielp));
                pTemp.Add(new Point(dminPoz, dmin * (double)instrumentKarta.podzielp));
            }

            /*
            //(0.0001/1.5000) x 10,000 = 0.6666
            double pip =(double)(instrumentKarta.info.ContractSize) ;
            pip /= (double)instrumentKarta.info.Ask;
            pip *= (double)instrumentKarta.info.LotMin;
            */
            tb_wyniktekst.Text = opis;
            RysujWykres(razem, cWykresRazem, Brushes.Red, 1, pTemp);
        }
        private void dispatcherTimer_Update(object sender, EventArgs e)
        {
            instrumentKarta.DaneInstrumentu();
            string cyfr = instrumentKarta.ulamek.ToString("D");
            tb_dzmax.Text = ((double)instrumentKarta.info.High).ToString("F0" + cyfr);
            tb_dzmin.Text = ((double)instrumentKarta.info.Low).ToString("F0" + cyfr);
            tb_bid.Text = ((double)instrumentKarta.info.Bid).ToString("F0" + cyfr);
            tb_ask.Text = ((double)instrumentKarta.info.Ask).ToString("F0" + cyfr);
            tb_symbol.Text = instrumentKarta.info.Symbol.ToString();
            tb_opis.Text = instrumentKarta.info.Description.ToString();
        }
        private void TreeViewItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var item = (TextBlock)sender;
           
            SplashTxt("..CZEKAJ..");
            instrumentKarta = new InstrumentKarta(item.Text);
            instrumentKarta.DaneInstrumentu();
            instrumentKarta.Notowania(PERIOD_CODE.PERIOD_M1, 17800);

            string cyfr = instrumentKarta.ulamek.ToString("D");
            tb_dzmax.Text = ((double)instrumentKarta.info.High).ToString("F0" + cyfr);
            tb_dzmin.Text = ((double)instrumentKarta.info.Low).ToString("F0" + cyfr);
            tb_bid.Text = ((double)instrumentKarta.info.Bid).ToString("F0" + cyfr);
            tb_ask.Text = ((double)instrumentKarta.info.Ask).ToString("F0" + cyfr);

            tb_symbol.Text = instrumentKarta.info.Symbol.ToString();
            tb_opis.Text = instrumentKarta.info.Description.ToString();

            RysujWykres(instrumentKarta.danewykres(200), cWykresRazem, Brushes.Red, 0, null);
            
            panelSieci.Visibility = Visibility.Visible;
            
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Update;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
            dispatcherTimer.Start();

        }
    }
    public class NeuralNetwork
    {
        private int numInput; // number input nodes
        private int numHidden;
        private int numOutput;

        private double[] inputs;
        private double[][] ihWeights; // input-hidden
        private double[] hBiases;
        private double[] hOutputs;

        private double[][] hoWeights; // hidden-output
        private double[] oBiases;
        private double[] outputs;

        private Random rnd;

   

        private delegate void NoArgDelegate();
        public NeuralNetwork(int numInput, int numHidden, int numOutput)
        {
            this.numInput = numInput;
            this.numHidden = numHidden;
            this.numOutput = numOutput;

            this.inputs = new double[numInput];

            this.ihWeights = MakeMatrix(numInput, numHidden, 0.0);
            this.hBiases = new double[numHidden];
            this.hOutputs = new double[numHidden];

            this.hoWeights = MakeMatrix(numHidden, numOutput, 0.0);
            this.oBiases = new double[numOutput];
            this.outputs = new double[numOutput];

            this.rnd = new Random(0);
            this.InitializeWeights(); // all weights and biases
        } // ctor

        private static double[][] MakeMatrix(int rows, int cols, double v) // helper for ctor, Train
        {
            double[][] result = new double[rows][];
            for (int r = 0; r < result.Length; ++r) result[r] = new double[cols];
            for (int i = 0; i < rows; ++i) for (int j = 0; j < cols; ++j) result[i][j] = v;
            return result;
        }

        private void InitializeWeights() // helper for ctor
        {
            // initialize weights and biases to small random values
           
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            double[] initialWeights = new double[numWeights];
            for (int i = 0; i < initialWeights.Length; ++i)
            {
                initialWeights[i] = (0.0001 - 0.00001) * rnd.NextDouble() + 0.00001; //oil
                //initialWeights[i] = (0.001 - 0.0001) * (rnd.NextDouble() - 0.0001);
                //initialWeights[i] = (0.01 - 0.001) * rnd.NextDouble() + 0.001;

            }
            this.SetWeights(initialWeights);
            

            /*
             
              // initialize weights and biases to small random values
      int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
      double[] initialWeights = new double[numWeights];
      double lo = -0.01;
      double hi = 0.01;
      for (int i = 0; i < initialWeights.Length; ++i)
        initialWeights[i] = (hi - lo) * rnd.NextDouble() + lo;
      this.SetWeights(initialWeights);
             
             

            */


        }

        public int[] Konfig()
        {
            int[] ret = new int[3];
            ret[0] = numInput;
            ret[1] = numHidden;
            ret[2] = numOutput;
            return ret;
        }

        public void SetWeights(double[] weights)
        {
            // copy serialized weights and biases in weights[] array
            // to i-h weights, i-h biases, h-o weights, h-o biases
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            if (weights.Length != numWeights) throw new Exception("Bad weights array in SetWeights");

            int k = 0; // points into weights param

            for (int i = 0; i < numInput; ++i) for (int j = 0; j < numHidden; ++j) ihWeights[i][j] = weights[k++];
            for (int i = 0; i < numHidden; ++i) hBiases[i] = weights[k++];

            for (int i = 0; i < numHidden; ++i) for (int j = 0; j < numOutput; ++j) hoWeights[i][j] = weights[k++];
            for (int i = 0; i < numOutput; ++i) oBiases[i] = weights[k++];
        }

        public double[] GetWeights()
        {
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            double[] result = new double[numWeights];
            int k = 0;

            for (int i = 0; i < ihWeights.Length; ++i) for (int j = 0; j < ihWeights[0].Length; ++j) result[k++] = ihWeights[i][j];
            for (int i = 0; i < hBiases.Length; ++i) result[k++] = hBiases[i];

            for (int i = 0; i < hoWeights.Length; ++i) for (int j = 0; j < hoWeights[0].Length; ++j) result[k++] = hoWeights[i][j];
            for (int i = 0; i < oBiases.Length; ++i) result[k++] = oBiases[i];

            return result;
        }

        public double[] ComputeOutputs(double[] xValues)
        {
            double[] hSums = new double[numHidden]; // hidden nodes sums scratch array
            double[] oSums = new double[numOutput]; // output nodes sums

            for (int i = 0; i < xValues.Length; ++i) this.inputs[i] = xValues[i];
            for (int j = 0; j < numHidden; ++j) for (int i = 0; i < numInput; ++i) hSums[j] += this.inputs[i] * this.ihWeights[i][j];
            for (int i = 0; i < numHidden; ++i) hSums[i] += this.hBiases[i];
            for (int i = 0; i < numHidden; ++i) this.hOutputs[i] = HyperTan(hSums[i]);
            for (int j = 0; j < numOutput; ++j) for (int i = 0; i < numHidden; ++i) oSums[j] += hOutputs[i] * hoWeights[i][j];
            for (int i = 0; i < numOutput; ++i) oSums[i] += oBiases[i];

            //double[] softOut = Softmax(oSums); 
            //Array.Copy(softOut, outputs, softOut.Length);

            Array.Copy(oSums, outputs, oSums.Length);

            double[] retResult = new double[numOutput];
            Array.Copy(this.outputs, retResult, retResult.Length);
            return retResult;

        }

        private static double HyperTan(double x)
        {

            if (x < -20.0) return -1.0; 
            else if (x > 20.0) return 1.0;
            else return Math.Tanh(x);
        }

        private static double[] Softmax(double[] oSums)
        {
            // does all output nodes at once so scale
            // doesn't have to be re-computed each time

            double sum = 0.0;
            double[] result = new double[oSums.Length];

            for (int i = 0; i < oSums.Length; ++i)
            {
                double expo = Math.Exp(oSums[i]);

                sum +=  expo;
            }

            for (int i = 0; i < oSums.Length; ++i)
            {
                double expo = Math.Exp(oSums[i]);
                result[i] =expo / (sum);
            }

            return result; // now scaled so that xi sum to 1.0
        }

        public double[] Train(double[][] trainData, int maxEpochs, double learnRate, double momentum, TextBox tb)
        {
            // train using back-prop
            // back-prop specific arrays
            double[][] hoGrads = MakeMatrix(numHidden, numOutput, 0.0);   // hidden-to-output weight gradients
            double[] obGrads = new double[numOutput];                   // output bias gradients

            double[][] ihGrads = MakeMatrix(numInput, numHidden, 0.0);    // input-to-hidden weight gradients
            double[] hbGrads = new double[numHidden];                   // hidden bias gradients

            double[] oSignals = new double[numOutput];                  // local gradient output signals - gradients w/o associated input terms
            double[] hSignals = new double[numHidden];                  // local gradient hidden node signals

            // back-prop momentum specific arrays 
            double[][] ihPrevWeightsDelta = MakeMatrix(numInput, numHidden, 0.0);
            double[] hPrevBiasesDelta = new double[numHidden];
            double[][] hoPrevWeightsDelta = MakeMatrix(numHidden, numOutput, 0.0);
            double[] oPrevBiasesDelta = new double[numOutput];

            int epoch = 0;
            double[] xValues = new double[numInput]; // inputs
            double[] tValues = new double[numOutput]; // target values
            double derivative = 0.0;
            double errorSignal = 0.0;

            int[] sequence = new int[trainData.Length];
            for (int i = 0; i < sequence.Length; ++i) sequence[i] = i;
            int errInterval = maxEpochs / 10; // interval to check error

            while (epoch < maxEpochs)
            {
                ++epoch;
                if (epoch % errInterval == 0 && epoch < maxEpochs)
                {
                    double trainErr = Error(trainData);
                    tb.AppendText("epoka = " + epoch + ", błąd = " + trainErr.ToString("F4") + "\n");
                    tb.CaretIndex = tb.Text.Length;
                    tb.ScrollToEnd();
                    tb.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (NoArgDelegate)delegate { });
                }

                Shuffle(sequence); // pomieszaj dane treningowe
                for (int ii = 0; ii < trainData.Length; ++ii)
                {
                    int idx = sequence[ii];

                    Array.Copy(trainData[idx], xValues, numInput);
                    Array.Copy(trainData[idx], numInput, tValues, 0, numOutput);

                    ComputeOutputs(xValues);
                    //wylicz dane 

                    // indices: i = inputs, j = hiddens, k = outputs

                    for (int k = 0; k < numOutput; ++k)
                    {
                        errorSignal = tValues[k] - outputs[k];  // Wikipedia uses (o-t)
                        //derivative = (1 - outputs[k]) * outputs[k]; // for softmax
                        oSignals[k] = errorSignal * outputs[k];// derivative;
                    }

                    // 2. compute hidden-to-output weight gradients using output signals
                    for (int j = 0; j < numHidden; ++j)
                        for (int k = 0; k < numOutput; ++k)
                            hoGrads[j][k] = oSignals[k] * hOutputs[j];

                    // 2b. compute output bias gradients using output signals
                    for (int k = 0; k < numOutput; ++k)
                        obGrads[k] = oSignals[k] * 1.0; // dummy assoc. input value

                    // 3. compute hidden node signals
                    for (int j = 0; j < numHidden; ++j)
                    {
                        derivative = (1 + hOutputs[j]) * (1 - hOutputs[j]); // for tanh
                        double sum = 0.0; // need sums of output signals times hidden-to-output weights
                        for (int k = 0; k < numOutput; ++k)
                        {
                            sum += oSignals[k] * hoWeights[j][k]; // represents error signal
                        }
                        hSignals[j] = derivative * sum;
                    }

                    // 4. compute input-hidden weight gradients
                    for (int i = 0; i < numInput; ++i)
                        for (int j = 0; j < numHidden; ++j)
                            ihGrads[i][j] = hSignals[j] * inputs[i];

                    // 4b. compute hidden node bias gradients
                    for (int j = 0; j < numHidden; ++j)
                        hbGrads[j] = hSignals[j] * 1.0; // dummy 1.0 input

                    // == update weights and biases

                    // update input-to-hidden weights
                    for (int i = 0; i < numInput; ++i)
                    {
                        for (int j = 0; j < numHidden; ++j)
                        {
                            double delta = ihGrads[i][j] * learnRate;
                            ihWeights[i][j] += delta; // would be -= if (o-t)
                            ihWeights[i][j] += ihPrevWeightsDelta[i][j] * momentum;
                            ihPrevWeightsDelta[i][j] = delta; // save for next time
                        }
                    }

                    // update hidden biases
                    for (int j = 0; j < numHidden; ++j)
                    {
                        double delta = hbGrads[j] * learnRate;
                        hBiases[j] += delta;
                        hBiases[j] += hPrevBiasesDelta[j] * momentum;
                        hPrevBiasesDelta[j] = delta;
                    }

                    // update hidden-to-output weights
                    for (int j = 0; j < numHidden; ++j)
                    {
                        for (int k = 0; k < numOutput; ++k)
                        {
                            double delta = hoGrads[j][k] * learnRate;
                            hoWeights[j][k] += delta;
                            hoWeights[j][k] += hoPrevWeightsDelta[j][k] * momentum;
                            hoPrevWeightsDelta[j][k] = delta;
                        }
                    }

                    // update output node biases
                    for (int k = 0; k < numOutput; ++k)
                    {
                        double delta = obGrads[k] * learnRate;
                        oBiases[k] += delta;
                        oBiases[k] += oPrevBiasesDelta[k] * momentum;
                        oPrevBiasesDelta[k] = delta;
                    }
                  
                } // each training item

            } // while
            double[] bestWts = GetWeights();
            return bestWts;
        } // Train

        private void Shuffle(int[] sequence) // instance method
        {
            for (int i = 0; i < sequence.Length; ++i)
            {
                int r = this.rnd.Next(i, sequence.Length);
                int tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        } // Shuffle

        private double Error(double[][] trainData)
        {
            // average squared error per training item
            double sumSquaredError = 0.0;
            double[] xValues = new double[numInput]; // first iloscWE values in trainData
            double[] tValues = new double[numOutput]; // last iloscWY values

            // walk thru each training case. looks like (6.9 3.2 5.7 2.3) (0 0 1)
            for (int i = 0; i < trainData.Length; ++i)
            {
                Array.Copy(trainData[i], xValues, numInput);
                Array.Copy(trainData[i], numInput, tValues, 0, numOutput); // get target values
                double[] yValues = this.ComputeOutputs(xValues); // outputs using current weights
                for (int j = 0; j < numOutput; ++j)
                {
                    double err = tValues[j] - yValues[j];
                    sumSquaredError += err * err;
                }
            }
            return sumSquaredError / trainData.Length;
        } // MeanSquaredError

        public double Accuracy(double[][] testData)
        {
            // percentage correct using winner-takes all
            int numCorrect = 0;
            int numWrong = 0;
            double[] xValues = new double[numInput]; // inputs
            double[] tValues = new double[numOutput]; // targets
            double[] yValues; // computed Y

            for (int i = 0; i < testData.Length; ++i)
            {
                Array.Copy(testData[i], xValues, numInput); // get x-values
                Array.Copy(testData[i], numInput, tValues, 0, numOutput); // get t-values
                yValues = this.ComputeOutputs(xValues);
                int maxIndex = MaxIndex(yValues); // which cell in yValues has largest value?
                int tMaxIndex = MaxIndex(tValues);

                if (maxIndex == tMaxIndex)
                    ++numCorrect;
                else
                    ++numWrong;
            }
            return (numCorrect * 1.0) / (numCorrect + numWrong);
        }

        private static int MaxIndex(double[] vector) // helper for Accuracy()
        {
            // index of largest value
            int bigIndex = 0;
            double biggestVal = vector[0];
            for (int i = 0; i < vector.Length; ++i)
            {
                if (vector[i] > biggestVal)
                {
                    biggestVal = vector[i];
                    bigIndex = i;
                }
            }
            return bigIndex;
        }

        public void ZapiszPlikNN(int ilEpok, double wartNauki, double momentum)
        {
            double[] wagi = GetWeights();
            int ilWag = wagi.Length;
            string[] plik = new string[ilWag + 7];

            plik[0] = numInput.ToString();
            plik[1] = numHidden.ToString();
            plik[2] = numOutput.ToString();
            plik[3] = wartNauki.ToString();
            plik[4] = momentum.ToString();
            plik[5] = ilEpok.ToString();
            plik[6] = ilWag.ToString();

            for (int i = 0; i < ilWag; i++) { plik[i + 7] = wagi[i].ToString(); }
            File.WriteAllLines(@"siec.nn", plik);
        }
    } // NeuralNetwork
    class PozTran
    {
        public string Nazwa { get; set; }
        public double CenaZak { get; set; }
        public double Zysk { get; set; }
        public string Rodzaj { get; set; }
    }
    public class InstrumentKarta
    {
        private protected string password = "HASLO";

        static Server serverData = Servers.REAL;
        static string userId = "XTB_ID";

        public double wyliczoneMIN { get; set; }
        public double wyliczoneMAX { get; set; }
        
        public int podzielp { get; set; }
        public int dziesiatki { get; set; }
        public int ulamek { get; set; }


        public string Nazwa;
        public double[] tabelaWynikow { get; set; }
        public xAPI.Records.SymbolRecord info { get; set; }
        public InstrumentKarta(String nazwaIns)
        {
            this.Nazwa = nazwaIns;
        }
        public double[] danewykres(long ilosc)
        {
            double[] ret = new double[ilosc];

            wyliczoneMIN = 99999999;
            wyliczoneMAX = 0;

            for (int i = 0; i < ilosc; i++)
            {
                ret[i] = tabelaWynikow[(10000-ilosc) + i];
                if (ret[i] > wyliczoneMAX) wyliczoneMAX = ret[i];
                if (ret[i] < wyliczoneMIN) wyliczoneMIN = ret[i];
            }
         
            return ret;
        }
        public void DaneInstrumentu()
        {
            try
            {
                SyncAPIConnector connector = new SyncAPIConnector(serverData);
                Credentials credentials = new Credentials(userId, password, "", "pixelNet");
                LoginResponse loginResponse = APICommandFactory.ExecuteLoginCommand(connector, credentials, true);
                SymbolResponse symbolResponse = APICommandFactory.ExecuteSymbolCommand(connector, this.Nazwa);
                this.info = symbolResponse.Symbol;
                ulamek = Convert.ToInt32(info.Precision);
                double d = Convert.ToDouble(info.Ask);
                string strl = d.ToString("0");
                dziesiatki = strl.Length;
                podzielp = Convert.ToInt32(Math.Pow(10, strl.Length));

                LogoutResponse logoutResponse = APICommandFactory.ExecuteLogoutCommand(connector);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public void Notowania(PERIOD_CODE pc, long Zakres)
        {
            tabelaWynikow = new double[10000];
            try
            {
                SyncAPIConnector connector = new SyncAPIConnector(serverData);
                Credentials credentials = new Credentials(userId, password, "", "pixelNet");
                LoginResponse loginResponse = APICommandFactory.ExecuteLoginCommand(connector, credentials, true);

                double ms = DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds;
                long? msd = Convert.ToInt64(ms);
                long? msd1 = msd - (Zakres * (60 * 1000));//17800 10000min
                ChartRangeInfoRecord zakres = new ChartRangeInfoRecord(Nazwa, pc, msd1, msd, 0);
                ChartRangeResponse chartRangeResponse = APICommandFactory.ExecuteChartRangeCommand(connector, zakres);

                int tabdl = 0;
                int dlresp = chartRangeResponse.RateInfos.Count - 10000;

                double podz = dziesiatki + ulamek;
                podz = Math.Pow(10, podz);
                
                foreach (xAPI.Records.RateInfoRecord nota in chartRangeResponse.RateInfos)
                {
                    if (tabdl >= dlresp)
                    {
                        tabelaWynikow[tabdl - dlresp] = (double)nota.Open / podz;
                    }
                    tabdl++;
                }
                LogoutResponse logoutResponse = APICommandFactory.ExecuteLogoutCommand(connector);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
    public class ListaInstrumentow
    {
        public ListaInstrumentow()
        {
            this.Pozycje = new ObservableCollection<SymbolRecord>();
        }

        public string Symbol { get; set; }

        public ObservableCollection<SymbolRecord> Pozycje { get; set; }
    }
}

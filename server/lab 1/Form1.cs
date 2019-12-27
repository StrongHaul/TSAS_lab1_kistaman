using System;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace lab_1
{
	public partial class Form1 : Form
	{
		public double[] Tsreda = new double[1];         // истинная температура среды
		public double[] Filtered = new double[] { 18 };       // сглаженная температура среды

		public double ventilator = 50,         // частота вращения вентилятора
			vTeds,               // термоэдс
			vTsreda = 23,         // температура среды
			t_sensor,
			filtered;
		public bool speed2 = true;        // усиленный режим вентилятора вкл
		public double Tmax = 25,        // верхний гистерезис
			Tmin = 17;                  // нижний гистерезис
		public int h = 4;       // шаг сглаживания
		Random random = new Random();
		GraphPane pane1;     // объект для рисования графиков
		LineItem line_chastota, line_termoeds, line_tistinnay, line_t_datchik, line_filtered;   // линии
		PointPairList point_chastota = new PointPairList();
		PointPairList point_termoeds = new PointPairList();
		PointPairList point_t_istionnay_sreda = new PointPairList();
		PointPairList point_sensor = new PointPairList();
		PointPairList point_filtered = new PointPairList();       // списки с точками
		int t = 0;              // счетчик времени

		public Form1()
		{
			InitializeComponent();

			pane1 = z1.GraphPane;
			z1.GraphPane.CurveList.Clear();

			z1.IsShowPointValues = true;
			z1.IsEnableHZoom = true;
			z1.IsEnableVZoom = true;

			pane1.XAxis.Title.IsVisible = false;
			pane1.YAxis.Title.IsVisible = false;
			pane1.XAxis.Scale.IsSkipFirstLabel = true;
			pane1.XAxis.Scale.IsSkipLastLabel = true;
			pane1.XAxis.Scale.IsSkipCrossLabel = true;
			pane1.YAxis.Scale.IsSkipLastLabel = true;
			pane1.YAxis.Scale.IsSkipCrossLabel = true;
			pane1.Title.IsVisible = false;

			//pane1.XAxis.Cross = 0.0;
			//pane1.YAxis.Cross = 0.0;

			pane1.XAxis.Scale.MinAuto = true;
			pane1.XAxis.Scale.MaxAuto = true;
			pane1.YAxis.Scale.MinAuto = true;
			pane1.YAxis.Scale.MaxAuto = true;

			pane1.IsBoundedRanges = true;    // при автоматическом подборе масштаба нужно учитывать только видимый интервал графика

			line_chastota = z1.GraphPane.AddCurve("Частота вращения ветилятора (об/с)", point_chastota, Color.Orange, SymbolType.None);      // частота вращения
			line_chastota.Line.Width = 2;
			line_chastota.Line.IsSmooth = true;

			line_t_datchik = z1.GraphPane.AddCurve("Температура в комнате (датчик)", point_sensor, Color.Red, SymbolType.None);      // температура (датчик) с погрешностью
			line_t_datchik.Line.Width = 2;
			line_t_datchik.Line.IsSmooth = true;

			line_termoeds = z1.GraphPane.AddCurve("ЭДС", point_termoeds, Color.Blue, SymbolType.None);       // эдс
			line_termoeds.Line.Width = 2;
			line_termoeds.Line.IsSmooth = true;

			line_tistinnay = z1.GraphPane.AddCurve("Истинная температура среды", point_t_istionnay_sreda, Color.Green, SymbolType.None);      // истинная температура
			line_tistinnay.Line.Width = 2;
			line_tistinnay.Line.IsSmooth = true;

			line_filtered = z1.GraphPane.AddCurve("Сглаженная температура от датчика", point_filtered, Color.Purple, SymbolType.None);      // сглаженный график
			line_filtered.Line.Width = 2;
			line_filtered.Line.IsSmooth = true;


			z1.AxisChange();
			z1.Invalidate();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			timer1.Stop();
			timer1.Interval = 1000;
		}

		private void button2_Click(object sender, EventArgs e)      // нажимаем на кнопку запустить 
		{
			timer1.Start();
			button2.Enabled = false;

		}

		private void Timer1_Tick(object sender, EventArgs e)        // таймер
		{
			t++;
			Climate();
			Sensor();
			Processing();
		}

		private void Climate()
		{

			if (vTsreda >= Tmax)
			{
				speed2 = true;
			}

			if (vTsreda <= Tmin)
			{
				speed2 = false;
			}
			if (speed2 == true)      // если вторая скорость вентилятора включена
			{
				ventilator -= Math.Abs(30 - ventilator) / 3;
				double dT = Math.Log(Math.Abs(vTsreda - (Tmin - 2)));
				vTsreda -= dT;
			}
			else
			{
				ventilator += Math.Abs(50 - ventilator) / 3;
				double dT = Math.Log(Math.Abs(vTsreda - (Tmax + 1)));
				vTsreda += dT;
			}
		}

		private void Sensor()    //  датчик, выдает термоэдс
		{
			vTeds = vTsreda * 0.2;
			t_sensor = vTsreda + random.Next(-1, 2);
		}

		private void Processing()   // получить данные
		{
			Array.Resize(ref Tsreda, Tsreda.Length + 1);

			Tsreda[Tsreda.Length - 1] = vTsreda;

			filtered = Smooth();

			this.Invoke(new MethodInvoker(delegate
				{
					dataGridView1.Rows.Add(dataGridView1.Rows.Count + 1, null, filtered, null);
					dataGridView1.FirstDisplayedCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0];
					Graphics();
				}));
		}

		private double Smooth()       // сглаживание данных от датчика
		{
			if (dataGridView1.Rows.Count <= h)
				return 0;
			Array.Resize(ref Filtered, Filtered.Length + 1);    // увеличили длину массива Filtered на 1
			double filter_current = Filtered[Filtered.Length - 2];      // последнее отфильтрованное значение
			double teds_Current_Minus_Step = Tsreda[Tsreda.Length - 1 - h];
			double filter_next = filter_current - (teds_Current_Minus_Step / h) + (Tsreda[Tsreda.Length - 1] / h);    // новое отфильтрованное значение
			Filtered[Filtered.Length - 1] = filter_next;

			return filter_next;
		}

		private void Graphics()     // отображение графика
		{
			point_chastota.Add(t, ventilator);
			point_termoeds.Add(t, vTeds);
			point_filtered.Add(t, filtered);
			point_t_istionnay_sreda.Add(t, vTsreda);
			point_sensor.Add(t, t_sensor);

			z1.AxisChange();
			z1.Invalidate();
		}

	}

}


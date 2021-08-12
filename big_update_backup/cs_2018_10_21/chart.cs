<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>

<script language=C# runat=server>

class LineChart
{
	//Sample ASPX C# LineChart Class, Steve Hall, 2002
	public Bitmap b;
	public string Title="Default Title";
	public ArrayList chartValues = new ArrayList();
	public float Xorigin=0, Yorigin=0;
	public float ScaleX, ScaleY;
	public float Xdivs=2, Ydivs=2;

	private int Width, Height;
	private Graphics g;
	private Page p;
	private int m_nBgColor = 0xFFFFFF; //white

	struct datapoint 
	{
		public float x;
		public float y;
		public bool valid;
	}

	//initialize
	public LineChart(int myWidth, int myHeight, Page myPage) 
	{
		Width = myWidth; Height = myHeight;
		ScaleX = myWidth; ScaleY = myHeight;
		b = new Bitmap(myWidth, myHeight);
		g = Graphics.FromImage(b);
		p = myPage;
//		p.Response.ContentType="image/jpeg";
	}

	public void SetBgColor(int color)
	{
		m_nBgColor = color;
	}

	public void AddValue(int x, int y) 
	{
		datapoint myPoint;
		myPoint.x=x;
		myPoint.y=y;
		myPoint.valid=true;
		chartValues.Add(myPoint);
	}
	
	public void Save(string file)
	{
		try //in case write failed eg:file is readonly
		{
			b.Save(file);
		}
		catch(Exception e)
		{
		}
	}

	public void Draw() 
	{
		int i;
		float x, y, x0, y0;
		string myLabel;
		Pen blackPen = new Pen(Color.Black,1);
		Pen bluePen = new Pen(Color.Blue,0);
		Pen greenPen = new Pen(Color.Green,0);
		Pen redPen = new Pen(Color.Red,1);
		Brush blackBrush = new SolidBrush(Color.Black);
		Font axesFont = new Font("vardana",8);

		//first establish working area
		g.FillRectangle(new	SolidBrush(Color.FromArgb(0x78000000 + m_nBgColor)),0,0,Width,Height);
//		g.FillRectangle(new	SolidBrush(Color.White),0,0,Width,Height);
		int ChartInset = 35;
		int ChartWidth = Width-(2*ChartInset);
		int ChartHeight = Height-(2*ChartInset);
		g.DrawRectangle(blackPen,ChartInset,ChartInset,ChartWidth,ChartHeight);
		g.DrawString(Title, new Font("vardana",8), blackBrush, 6, 10);
		DateTime dNow = DateTime.Now;
		DateTime dOrigin = dNow.AddYears(-1);	//backward one year
		dOrigin = dOrigin.AddDays(0 - dNow.Day);//back to the beginning of the month

		DateTime d = dOrigin;
		DateTime dEnd = dNow.AddMonths(2);
		while(d < dEnd)
		{
			x=ChartInset + (d - dOrigin).Days;
			y=ChartHeight+ChartInset;
//g.DrawString("x=" + x.ToString(), axesFont, blackBrush, 300, y-i*10);
			if(d==dOrigin || d.Month == 1 || d.Month == 2)
				myLabel = d.ToString("MMM yy");
			else
				myLabel = d.ToString("MMM");
			g.DrawString(myLabel, axesFont, blackBrush, x-10, y+10);
			g.DrawLine(blackPen, x, y+3, x, y);
			g.DrawLine(new Pen(Color.FromArgb(0x078CCCCCC), 0), x, y, x, ChartInset);
			d = d.AddMonths(2);
		}

		//draw Y axis labels
		for(i=0; i<=Ydivs; i++) 
		{
			x=ChartInset;
			y=ChartHeight+ChartInset-(i*ChartHeight/Ydivs);
			myLabel = (Yorigin + (ScaleY*i/Ydivs)).ToString();
			g.DrawString(myLabel, axesFont, blackBrush, 5, y-6);
			g.DrawLine(blackPen, x, y, x-3, y);
			int m = ChartInset + 4;
			while(m < ChartWidth + ChartInset)
			{
				g.DrawLine(greenPen, m, y, m+3, y);
				m += 7;
			}
		}

		//transform drawing coords to lower-left (0,0)
		g.RotateTransform(180);
		g.TranslateTransform(0,-Height);
		g.TranslateTransform(-ChartInset,ChartInset);
		g.ScaleTransform(-1, 1);

		//draw chart data
		datapoint prevPoint = new datapoint();
		prevPoint.valid=false;
//int py = 30;
		foreach(datapoint myPoint in chartValues) 
		{
			if(prevPoint.valid==true) 
			{
				x0=ChartWidth*(prevPoint.x-Xorigin)/ScaleX;
				y0=ChartHeight*(prevPoint.y-Yorigin)/ScaleY;
				x=ChartWidth*(myPoint.x-Xorigin)/ScaleX;
				y=ChartHeight*(myPoint.y-Yorigin)/ScaleY;
//g.DrawString("x0="+x0 + ", y0="+y0 +  ", x="+x + ", y="+ y, axesFont, blackBrush, 30, py);
//py -= 10;
				g.DrawLine(redPen,x0,0,x,0);
				g.DrawLine(redPen,x0,y0,x,y);
				g.FillEllipse(blackBrush,x0-4,y0-4,8,8);
				g.FillEllipse(blackBrush,x-4,y-4,8,8);
			}
			prevPoint = myPoint;
		}

		//finally send graphics to browser
//		b.Save(p.Response.OutputStream, ImageFormat.Jpeg);
	}

	~LineChart() 
	{
		g.Dispose();
		b.Dispose();
	}
}
</script> 

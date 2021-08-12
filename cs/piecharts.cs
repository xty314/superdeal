<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>

<script language=C# runat=server>
public class piecharts
{
	public piecharts()
	{
		//
		// TODO: Add constructor logic here
		//
	}

	public Bitmap GetPieChart(DataRowCollection drc, string dataColumnName, string labelColumnName, string title, int width)
	{
		// Get Sum of All Number Data
		float total = 0.0F;
		total = GetTotal(drc, dataColumnName);
		if(total <=0)
			return null;

		// Create Fonts
		Font fontLegend = new Font("Verdana", 10);
		Font fontTitle = new Font("Verdana", 15, FontStyle.Bold);

		// Get Largest Label in drc
		SizeF longestLabel = FindLongestLabelLength(drc, fontLegend, labelColumnName);

		// Create a Pen to Draw Lines With
		Pen labelPen = new Pen(Color.Black, 1);

		// Determine deminsions of bitmap
		const int bufferSpace = 30;
		int legendHeight = 0;//fontLegend.Height * (drc.Count + 1) + bufferSpace;
		int titleHeight = fontTitle.Height + (bufferSpace*2);
		int height = width + legendHeight + titleHeight + bufferSpace;
		int pieHeight = width;
        int outerWidth = width * 2;

		int marginWidth = (outerWidth - width) / 2;
		if (Convert.ToInt32(longestLabel.Width) > marginWidth )
		{
			outerWidth += ((Convert.ToInt32(longestLabel.Width) - marginWidth) * 2);

			// recalc marginWidth
			marginWidth = (outerWidth - width) / 2;
		}

		double radius = width / 2;

		// Get Center Point
		float centerX = (outerWidth / 2);
		float centerY = (pieHeight / 2) + titleHeight;
		Point centerPoint = new Point(Convert.ToInt32(centerX), Convert.ToInt32(centerY));

		Rectangle pieRect = new Rectangle(Convert.ToInt32(marginWidth), titleHeight, width, pieHeight);
		
		// Get List of Colors
		ArrayList colors = GetColors(drc.Count);

		// Draw Pie Chart Here
		Bitmap myBitmap = new Bitmap(outerWidth, height);
		Graphics myGraphics = Graphics.FromImage(myBitmap);
		myGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
		myGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

		SolidBrush blackBrush = new SolidBrush(Color.Black);
		
		myGraphics.FillRectangle(new SolidBrush(Color.White), 0, 0, outerWidth, height);

		float currentDegree = 0.0F;
		float mySweepAngle = 0.0F;
		double midPointDegree = 0.0F;
		int iLoop = 0;

		for (iLoop=0; iLoop<drc.Count; iLoop++)
		{
			// Determine the sweep angle for slice
			mySweepAngle = Convert.ToSingle(drc[iLoop][dataColumnName]) / total * 360;

			// Draw and Fill Slice
//			myGraphics.FillPie((SolidBrush) colors[iLoop], pieRect, currentDegree, mySweepAngle);
			myGraphics.FillPie((SolidBrush) colors[iLoop], pieRect, currentDegree, mySweepAngle);
			
			// Write Label for Slice
			midPointDegree = currentDegree + (mySweepAngle / 2);
			Point newPoint = GetPoint(Convert.ToDouble(centerX), Convert.ToDouble(centerY), midPointDegree, radius, radius);

			SizeF stringSize = myGraphics.MeasureString(drc[iLoop][labelColumnName].ToString(), fontLegend);
			if (newPoint.X > Convert.ToInt32(centerX)) 
			{
				// Quadrant 1
				if (newPoint.Y < Convert.ToInt32(centerY))
				{
					newPoint.Y -= Convert.ToInt32(stringSize.Height / 2);
					newPoint.X += 2;
				}
				
				// Quadrant 2
				// Do Nothing

			}
			else
			{
				// Quadrent 3 and 4
				newPoint.X -= Convert.ToInt32(stringSize.Width);
				
				// Quadrant 4
				if (newPoint.Y < Convert.ToInt32(centerY))
				{
					newPoint.Y -= Convert.ToInt32(stringSize.Height);
				}
			}

			if (mySweepAngle >= 7.2)
			{
				myGraphics.DrawString((drc[iLoop][labelColumnName].ToString()), fontLegend, blackBrush, newPoint);
			}

			// Increment the currentDegree	
			currentDegree += mySweepAngle;
		}
/*
		// Create Title
		StringFormat fmt = new StringFormat();
		fmt.Alignment = StringAlignment.Center;
		fmt.LineAlignment = StringAlignment.Center;
		myGraphics.DrawString(title, fontTitle, blackBrush, new Rectangle(0, 0, outerWidth, titleHeight), fmt);

		// Create Legend
		myGraphics.DrawRectangle(new Pen(Color.Black, 2), 0, height - legendHeight, outerWidth, legendHeight);

		for(iLoop=0; iLoop<drc.Count; iLoop++)
		{
			myGraphics.FillRectangle((SolidBrush) colors[iLoop], 5, height - legendHeight + fontLegend.Height * iLoop + 5, 10, 10);
			double perc = Convert.ToDouble(drc[iLoop][dataColumnName]) / Convert.ToDouble(total);
			perc = perc * 100;
			perc = Math.Round(perc, 2);
			myGraphics.DrawString((drc[iLoop][labelColumnName].ToString()) + " - " + Convert.ToString(drc[iLoop][dataColumnName]) + " (" + perc.ToString() + " %)", fontLegend, blackBrush, 20, height - legendHeight + fontLegend.Height * iLoop + 1);
		}
		
		myGraphics.DrawString("Total: " + Convert.ToString(total), fontLegend, blackBrush, 5, height - fontLegend.Height - 5);
*/	
		return myBitmap;
	}


	private SizeF FindLongestLabelLength(DataRowCollection drc, Font f, string lblCol)
	{
		SizeF longest = new SizeF();
		Bitmap img = new Bitmap(1, 1);
		Graphics tempGraphics = Graphics.FromImage(img);
		int iLoop;

		for (iLoop=0; iLoop<drc.Count; iLoop++)
		{
			if (longest.Width < tempGraphics.MeasureString(drc[iLoop][lblCol].ToString(), f).Width)
			{
				longest = tempGraphics.MeasureString(drc[iLoop][lblCol].ToString(), f);
			}
		}
		return longest;
	}
	
	private ArrayList GetColors(int num)
	{
		// Make first 5 colors static
		ArrayList colors = new ArrayList();
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(153, 204, 153)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(153, 153, 204)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 153, 153)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(51, 204, 204)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 204, 51)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 51, 204)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 153, 153)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(153, 153, 0)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(153, 0, 153)));
//		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 153)));

		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 255, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 255)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 255, 255)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 255)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 204)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 204, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 0, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 204, 204)));

		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(153, 0, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 153, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 204, 204))); 
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 204, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 0, 204)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 204)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 0, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 204, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(204, 204, 204)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 102, 102)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(102, 102, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(102, 0, 102)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 102)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(102, 0, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 102, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 255, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 255)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 255)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(0, 255, 255)));
		colors.Add(new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 0)));
		
		if (num > 31) 
		{
			// Add additional colors
			int addNum = num - 31;
			int iLoop = 0;
			Random rnd = new Random();

			for (iLoop=0; iLoop<addNum+1; iLoop++)
			{
				int n = rnd.Next(30);
				colors.Add(colors[n]);
			}
		}

		return colors;
	}


	private string GetColorRGB(Color c)
	{
		string colorRGB = c.R.ToString() + "|" + c.G.ToString() + "|" + c.B.ToString();
		return colorRGB;
	}


	private Point GetPoint(double CenterX, double CenterY, double degree, double radiusX, double radiusY)
	{
		double convert = 0.0F;
		double X = 0.0F;
		double Y = 0.0F;

		degree += 180;
		if (degree > 360)
		{
			degree -= 360;
		}

		convert = Math.PI / 180.0;
		//X = CenterX - (Math.Sin(Convert.ToDouble(-degree * convert)) * radiusX);
		//Y = CenterY - (Math.Sin(Convert.ToDouble((90 + degree) * convert)) * radiusY);
		X = CenterX - (radiusX * Math.Cos(convert * degree));
		Y = CenterY - (radiusY * Math.Sin(convert * degree));

		Point myPoint = new Point(Convert.ToInt32(X), Convert.ToInt32(Y));
		return myPoint;
	}

	// Gets total for pie chart where the columnName is value column of the pie chart
	private float GetTotal(DataRowCollection drc, string columnName)
	{
		float total = 0.0F, tmp;
		int iLoop;
		for (iLoop=0; iLoop < drc.Count; iLoop++)
		{
			tmp = Convert.ToSingle(drc[iLoop][columnName]);
			total += tmp;
		}
		return total;
	}
}
</script>
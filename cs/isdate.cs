<script language=C# runat=server>
 
public static bool IsDate(object dt)
{
  try
  {
	  string format = "dd-mm-yyyy";
	  System.Globalization.DateTimeFormatInfo dtfmi = new System.Globalization.DateTimeFormatInfo();
	  dtfmi.ShortDatePattern = format;
	  System.DateTime.ParseExact(dt.ToString(), "d", dtfmi);
	  
	  return true;

  }
  catch
  {
    return false;
  }

}
public static bool IsDateLong(object dt)
{
  try
  {
	  string format = "dd-mm-yy";
	  System.Globalization.DateTimeFormatInfo dtfmi = new System.Globalization.DateTimeFormatInfo();
	  dtfmi.ShortDatePattern = format;
	  System.DateTime.ParseExact(dt.ToString(), "d", dtfmi);
	  
	  return true;

  }
  catch
  {
    return false;
  }

}
public static bool IsDateShort(object dt)
{
  try
  {
	  string format = "dd-m-yy";
	  System.Globalization.DateTimeFormatInfo dtfmi = new System.Globalization.DateTimeFormatInfo();
	  dtfmi.ShortDatePattern = format;
	  System.DateTime.ParseExact(dt.ToString(), "d", dtfmi);
	  
	  return true;

  }
  catch
  {
    return false;
  }

}
/*
public static bool IsDateNoDash(object dt)
{
  try
  {
	  string format = "ddmmyyyy";
	  System.Globalization.DateTimeFormatInfo dtfmi = new System.Globalization.DateTimeFormatInfo();
	  dtfmi.ShortDatePattern = format;
	  System.DateTime.ParseExact(dt.ToString(), "d", dtfmi);
	  
	  return true;

  }
  catch
  {
    return false;
  }

}
public static bool IsDateNoDashShort(object dt)
{
  try
  {
	  string format = "ddmmyy";
	  System.Globalization.DateTimeFormatInfo dtfmi = new System.Globalization.DateTimeFormatInfo();
	  dtfmi.ShortDatePattern = format;
	  System.DateTime.ParseExact(dt.ToString(), "d", dtfmi);
	  
	  return true;

  }
  catch
  {
    return false;
  }

}*/

</script>

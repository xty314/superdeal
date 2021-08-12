<!-- #include file="kit_fun.cs" -->
<script runat="server">

bool PrepareNewPurchase()
{
	CheckShoppingCart();
	EmptyCart();
/*
	Session["purchase_current_po_id"] = null;
	Session["purchase_current_po_number"] = null;
	Session["purchase_type"] = null;
	Session["purchase_status"] = null;
	Session["purchase_comment"] = null;
	Session["purchase_freight"] = null;
	Session["purchase_current_po_id"] = null;
	Session["purchase_inv"] = null;
	Session["purchase_supplierid"] = null;
	Session["purchase_billtoid"] = null;
	Session["purchase_currency"] = null;
	Session["purchase_exrate"] = "1";
	Session["purchase_gstrate"] = "0.125";
	Session["purchase_billtoid"] = null;
	Session["purchase_supplierid"] = null;
	Session["purchase_shipto"] = null;
*/
	return true;
}
</script>

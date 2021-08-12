var $jia = $(".num-jia");
var $jian = $(".num-jian");
$(document).on('click',".num-jia",function (e) {
   var orderNum;
   var $hasStock=$(this).parent().parent().parent().next().children()[1];
   var hasStock=$($hasStock).children()[0].innerText;
   var number=$(this).prev()[0].innerText;
   if(number=="0"){
      number=1;
   }
   
   var $span=$(this).parent().parent().parent().next().children()[0];
   var stockNum=$($span).next().next().val();
   if(hasStock=="√"&&stockNum!="0"){
      console.log(stockNum);
      var oldNum=$($span)[0].innerText;
      var newNum=parseInt(number)+parseInt(oldNum);
    
      if(newNum<=stockNum){
         orderNum=newNum;
         $($span).html(newNum);
      }else{
         orderNum=stockNum;
         $($span).html(stockNum);
      }
       
   var dataHerf=$(this).parent().parent().parent().next().next().find('.add_to_cart').attr('data-href')
   if(dataHerf.indexOf("&qty=")!=-1){
   
      dataHerf= dataHerf.split("&qty=")[0]+"&qty="+orderNum;
   }else{
      dataHerf= dataHerf+"&qty="+orderNum;
   }
   $(this).parent().parent().parent().next().next().find('.add_to_cart').attr('disabled',false);
   $(this).parent().parent().parent().next().next().find('.add_to_cart').attr('data-href',dataHerf);
   
   }




})
$(document).on('click',".num-jian",function (e) {
   var dataHerf=$(this).parent().parent().parent().next().next().find('.add_to_cart').attr('data-href');
    var number= $(this).next()[0].innerText;
    var orderNum;
    if(number=="0"){
       number=1;
    }
    var $span=$(this).parent().parent().parent().next().children()[0];
    var stockNum=$($span).next().next().val();
    console.log(stockNum);
    
   var oldNum=$($span)[0].innerText;  
   var newNum=parseInt(oldNum)-parseInt(number);
   if(newNum<=0){
    $($span).html(0);
    $(this).parent().parent().parent().next().next().find('.add_to_cart').attr('disabled',true);
     if(dataHerf.indexOf("&qty=")!=-1){
        dataHerf= dataHerf.split("&qty=")[0]+"&qty=0";
     }else{
        dataHerf= dataHerf+"&qty=0";
     }
   }else{
    $($span).html(newNum);
    orderNum=newNum
    if(dataHerf.indexOf("&qty=")!=-1){

      dataHerf= dataHerf.split("&qty=")[0]+"&qty="+orderNum;
   }else{
      dataHerf= dataHerf+"&qty="+orderNum;
   }
   }
   $(this).parent().parent().parent().next().next().find('.add_to_cart').attr('data-href',dataHerf)
   console.log(dataHerf);
  
 })
 $(function () {
  
    
var query=GetRequest();
var ca=query.c;

if(ca!=null||ca!=undefined){
   var category=ca.replace(/\+/g," ");

   $cate=$(".menu-categories").children();
   $.each($cate,function (i,item) {
      $target=$(item).children()[0];
     
     if($($target).html()==category){
        $($target).click()
     }   
   
    $subw= $(item).children()[1];
   $sub=$($subw).find("li")
    $.each($sub,function (k,ss){
            
      var sca=query.s; 
    
      
      var subcategory=sca.replace(/\+/g," ").toUpperCase();
  
      var starget=$($(ss).html()).html().toUpperCase();
      // console.log(starget);
      // console.log(subcategory);
    if(subcategory=="ZZZOTHERS"&&starget=="ALL OTHERS"){
 
       $($(ss).children()[0]).css("color","red");
    }
      
       if(starget==subcategory){

        
         //  $(ss).css("color","orange")
      $($(ss).children()[0]).css("color","red");
     
       }

    })
      
})



}



    //console.log($(".rrp_price"));
    $rrps=$(".rrp_price")
//console.log($rrps);
$.each($rrps,function (i,item) {
   var rrp=parseFloat($(item)[0].innerText.replace("$",""));
   var spp=parseFloat($(item).next()[0].innerText.replace("NZD","").trim().replace("$",""));
  var $sprice= $(item).children()[1];
  var sprice=$($sprice).val();
  
  
var special_date=$(item).next().next().val();
console.log(special_date);

  
   
   
   if(rrp<=spp){
      $(item).addClass("hidden");
      // $(item).parent().parent().parent().parent().find(".single_product_3").addClass("hidden");  
   }
   if(special_date==""||special_date==" "){
      $(item).parent().parent().parent().parent().find(".single_product_3").addClass("hidden");  
   }
})

var url=window.location.href;
if(url.indexOf("?")==-1){
   // window.location.href=url+"?";
   history.pushState(null,null,"?");
   // $("#sortSelect").addClass("hidden");
}else{
   
   if(url.indexOf("&asort")!=-1){
      var target=url.split("&asort=")[1];
      console.log(target);
      switch (target) {
         case "level_price0+desc":
            $("#sortSelect").val("pd");
            break;
            case "level_price0":
               $("#sortSelect").val("p");
               break;
               case "supplier_code+desc":
                  $("#sortSelect").val("sd");
                  break;
                  case "supplier_code":
                     $("#sortSelect").val("s");
                     break;
         default:
            break;
      }
     
   }
 
}

 
 })


$(document).on("change","#sortSelect",function (e) {
  
   var key= $(this).children('option:selected').val();  
   var urltemp=window.location.href;
   var res,url;
if(urltemp.indexOf("&asort=")!=-1){
url=urltemp.split("&asort=")[0];
}else{
   url=window.location.href;
}
   switch (key) {
      case "s":
         res=url+"&asort=supplier_code"
         break;
   
         case "sd":
            res=url+"&asort=supplier_code+desc"
            break;
         case "p":
            res=url+"&asort=level_price0"
            break;
         case "pd":
            res=url+"&asort=level_price0+desc"
            break;   
      default:
         break;
   }
   if(urltemp.indexOf("?")!=-1){
      window.location=res;
   }
 
     
})
//  slPageLast = WriteURLWithoutPageNumber() +
//  "&t=" + m_type +
//   "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + pages.ToString()+"&asort="+alex_sort;

function GetRequest() {
   var url = location.search;
   // console.log(url);
   var theRequest = new Object();
    if (url.indexOf("?") != -1) {
          var str = url.substr(1);
          strs = str.split("&");
          for (var i = 0; i < strs.length; i++) {
     if(strs[i].split("=")[0]=="categories"){
        theRequest[strs[i].split("=")[0]] = (decodeURIComponent(strs[i].split("=")[1])).split(",");
       
     }else if (strs[i].split("=")[0]=="enddatetime") {
       var temp=decodeURIComponent(strs[i].split("=")[1]);
        theRequest[strs[i].split("=")[0]] =moment(temp,"YYYY-MM-DD").add(1,"days").format("YYYY-MM-DD"); 
     }else if (strs[i].split("=")[0]=="branchids"){
        theRequest[strs[i].split("=")[0]]=(decodeURIComponent(strs[i].split("=")[1])).split(",");
     
   }else if (strs[i].split("=")[0]=="supplierids"){
      theRequest[strs[i].split("=")[0]]=(decodeURIComponent(strs[i].split("=")[1])).split(",");
   }else if(strs[i].split("=")[0]=="branchid"){
         if(strs[i].split("=")[1]!="-1"){
           theRequest[strs[i].split("=")[0]] = decodeURIComponent(strs[i].split("=")[1]);
         }
         
     }
     
     
     else if(strs[i].split("=")[0]=="subcategories"){
       var tempsub=strs[i].split("=")[1].split("*");
        theRequest[strs[i].split("=")[0]]=[];
       for (var j in tempsub){
         var tt=tempsub[j].split(",");
         theRequest[strs[i].split("=")[0]].push({
           "CategoryName":decodeURIComponent(tt[0]),
           "SubCategoryName":decodeURIComponent(tt[1])
         })
       }
     }
     else if(strs[i].split("=")[0]=="subsubcategories"){
       var tempsub=strs[i].split("=")[1].split("*");
        theRequest[strs[i].split("=")[0]]=[];
       for (var j in tempsub){
         var tt=tempsub[j].split(",");
         theRequest[strs[i].split("=")[0]].push({
           "CategoryName":decodeURIComponent(tt[0]),
           "SubCategoryName":decodeURIComponent(tt[1]),
           "subSubCategoryName":decodeURIComponent(tt[2]),
         })
       }
     }
     else{
        theRequest[strs[i].split("=")[0]] = decodeURIComponent(strs[i].split("=")[1]);
   
     }
             
   
          }
      }
      return theRequest;
   }
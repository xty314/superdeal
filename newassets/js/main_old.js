(function ($) {
 "use strict";
 
  /*----------------------------
 price-slider active
------------------------------ */
    var range = $('#slider-range');
    var amount = $('#amount');
    
	  range.slider({
	   range: true,
	   min: 2,
	   max: 300,
	   values: [ 2, 300 ],
	   slide: function( event, ui ) {
		amount.val( "$" + ui.values[ 0 ] + " - $" + ui.values[ 1 ] );
	   }
	  });
	  amount.val( "$" + range.slider( "values", 0 ) +
	   " - $" + range.slider( "values", 1 ) );   		

 /*----------------------------
 jQuery MeanMenu
------------------------------ */
jQuery('#mobile-menu-active').meanmenu();


/*----------------------
	 Carousel Activation
	----------------------*/ 
  $(".let_new_carasel").owlCarousel({
      autoPlay: true, 
	  slideSpeed:2000,
	  pagination:true,
	  navigation:true,	  
      items : 1,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-caret-left'></i>","<i class='fa fa-caret-right'></i>"],
      itemsDesktop : [1199,1],
	  itemsDesktopSmall : [980,1],
	  itemsTablet: [768,1],
	  itemsMobile : [767,1],
  });

 /*----------------------------
		Tooltip
    ------------------------------ */
    $('[data-toggle="tooltip"]').tooltip({
        animated: 'fade',
        placement: 'top',
        container: 'body'
    });
 /*----------------------------
  single portfolio activation
------------------------------ */ 
  $(".sub_pix").owlCarousel({
      autoPlay: true, 
	  slideSpeed:2000,
	  pagination:true,
	  navigation:false,	  
      items : 5,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,4],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,5],
	  itemsMobile : [767,3],
  });
 /*----------------------------
	toggole active
     ------------------------------ */
	$( ".all_catagories" ).on("click", function() {
	  $( ".cat_mega_start" ).slideToggle( "slow" );
	});
	
	$( ".showmore-items" ).on("click", function() {
	  $( ".cost-menu" ).slideToggle( "slow" );
	});


 
/*----------------------
	New  Products Carousel Activation
	----------------------*/ 
  $(".whole_product").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 3,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,3],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,1],
	  itemsMobile : [767,1],
  });

 /*----------------------
	Hot  Deals Carousel Activation
	----------------------*/  
  $(".new_cosmatic").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 1,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,1],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });

 /*---------------------
	 countdown
	--------------------- */
		$('[data-countdown]').each(function() {
		  var $this = $(this), finalDate = $(this).data('countdown');
		  $this.countdown(finalDate, function(event) {
			$this.html(event.strftime('<span class="cdown days"><span class="time-count">%-D</span> <p>Days</p></span> <span class="cdown hour"><span class="time-count">%-H</span> <p>Hour</p></span> <span class="cdown minutes"><span class="time-count">%M</span> <p>Min</p></span> <span class="cdown second"> <span><span class="time-count">%S</span> <p>Sec</p></span>'));
		  });
		});

  /*----------------------
        Products Catagory Carousel Activation
	----------------------*/ 
  $(".feature-carousel").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 4,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,3],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });

/*----------------------------
   Top Rate Carousel Activation
------------------------------ */  
  $(".all_ayntex").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 1,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,1],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });

/*----------------------------
   Featured Catagories Carousel Activation
------------------------------ */ 
  $(".achard_all").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 5,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,4],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,4],
	  itemsMobile : [767,2
],
  });

 /*----------------------------
   Blog Post Carousel Activation
 ------------------------------ */
  $(".blog_carasel").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 3,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,2],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });
 /*----------------------------
   Brand Logo Carousel Activation
------------------------------ */  
  $(".all_brand").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 6,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,4],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [480,2],
  });
/*----------------------
	scrollUp 
	$.scrollUp({
        scrollText: '<i class="fa fa-angle-double-up"></i></i><i class="fa fa-shopping-cart"></i>',
        easingType: 'linear',
        scrollSpeed: 900,
        animation: 'fade'
    });

	----------------------*/	

/*----------------------
	New  Products home-page-2 Carousel Activation
	----------------------*/  
  $(".product_2").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 4,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,3],
	  itemsDesktopSmall : [980,4],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });
  /*----------------------------
   Blog Post home-page-2 Carousel Activation
------------------------------ */ 
  $(".blog_new_carasel_2").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 2,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,2],
	  itemsDesktopSmall : [980,2],
	  itemsTablet: [768,1],
	  itemsMobile : [767,1],
  });

 /*----------------------------
   Products Catagory-2 Carousel Activation
------------------------------ */  
  $(".feature-carousel-2").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 2,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,2],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });

 /*----------------------------
   Blog Post home-page-3 Carousel Activation
------------------------------ */  
  $(".blog_carasel_5").owlCarousel({
      autoPlay: false, 
	  slideSpeed:2000,
	  pagination:false,
	  navigation:true,	  
      items : 4,
	  /* transitionStyle : "fade", */    /* [This code for animation ] */
	  navigationText:["<i class='fa fa-angle-left'></i>","<i class='fa fa-angle-right'></i>"],
      itemsDesktop : [1199,4],
	  itemsDesktopSmall : [980,3],
	  itemsTablet: [768,2],
	  itemsMobile : [767,1],
  });
/*-----------------------------
	Category Menu toggle
	-------------------------------*/
    $('.expandable a').on('click', function() {
        $(this).parent().find('.category-sub').toggleClass('submenu-active'); 
        $(this).toggleClass('submenu-active');  
        return false;  
    });	
	
/*----------------------------
  MixItUp:
------------------------------ */
	$('#Container') .mixItUp();

 /*----------------------------
 magnificPopup:
------------------------------ */	
	 $('.magnify').magnificPopup({type:'image'});
	 
	 
/*-------------------------
  Create an account toggle function
--------------------------*/
	 $( "#cbox" ).on("click", function() {
        $( "#cbox_info" ).slideToggle(900);
     });
	 
	 
	  $( '#showlogin, #showcoupon' ).on('click', function() {
			 $(this).parent().next().slideToggle(600);
		 }); 
	 
		 /*-------------------------
		   accordion toggle function
		 --------------------------*/
		 $('.payment-accordion').find('.payment-accordion-toggle').on('click', function(){
		   //Expand or collapse this panel
		   $(this).next().slideToggle(500);
		   //Hide the other panels
		   $(".payment-content").not($(this).next()).slideUp(500);
	 
		 });
		 /* -------------------------------------------------------
		  accordion active class for style
		 ----------------------------------------------------------*/
		 $('.payment-accordion-toggle').on('click', function(event) {
			 $(this).siblings('.active').removeClass('active');
			 $(this).addClass('active');
			 event.preventDefault();
		 });
	 
	 
	 
	
  

})(jQuery); 

$(function () {
	$(".inputnote, .inputqty").blur(function (e) {
		$("#uqpn").click();
	});
});
$(document).ready(function() {
  $('input.login').click(function () {
    var a = $('#alert');
      if ($('#email').val() == "" || $('#pass').val() == "") {
          a.html("Email or password can not be empty!").removeClass("alert-success").addClass("alert-danger");
          a.css("display","block");
          setTimeout(function () {
            a.css("display","none");
          }, 10000);
      }
      else {
          $.ajax({
              type: "POST",
              url: "login.aspx?t=j&a=login",
              data: "email=" + escape($('#email').val()) + "&pwd=" + escape($('#pass').val()),
              success: function (data) {
								eval('var info=' + data);
								var r = info.result;
								var m = info.msg;
                  if (r == "success") {
                    a.html("Login success.").removeClass("alert-danger").addClass("alert-success");
                    a.css("display","block");
                      setTimeout(function () {
                          a.css("display","none");
                          $('#login').click(); //after login success 2's, click login
                      }, 2000);
                  }
                  if (r != "success") {
                      a.html("Incorrect email or password.").removeClass("alert-success").addClass("alert-danger");
                      a.css("display","block");
                      setTimeout(function () {
                          a.css("display","none");
                      }, 10000);
                  }
              },
              error: function (XMLHttpRequest, textStatus, thrownError) {
              }
          });
      }
  });
});

$(function () {
	ajaxLoadItems();
	ajaxAddToCart();
});

function ajaxLoadItems() {
	$('#ajax_shopping_cart').html("");
	$('#qty_badge').html("");
  
  var cartHeaderAndFooter = '<div class="header_all shopping_cart_area"><div class="widget_shopping_cart_content"><div class="topcart">';
  cartHeaderAndFooter += '<a class="cart-toggler" href="javascript:void(0)"><i class="icon"></i>';
  cartHeaderAndFooter += '<span class="qty" style="position: relative;top: -10px;font-size: 20px;">qty_total Items</span>';
  cartHeaderAndFooter += '<span class="my-cart">Shopping cart</span><span class="fa fa-angle-down"></span></a>';
  cartHeaderAndFooter += '<div class="new_cart_section"><ol class="new-list">new_list';
  cartHeaderAndFooter += '</ol><div class="top-subtotal">Sub Total: <span class="sig-price">price_total</span><br>';
  cartHeaderAndFooter += 'Total Tax: <span class="sig-price">price_tax</span><br>';
  cartHeaderAndFooter += 'Total Amount Due: <span class="sig-price">price_amount</span>';
  cartHeaderAndFooter += '</div><div class="cart-button"><ul><li class="pull-right"><a href="cart.aspx">View my cart <i class="fa fa-angle-right"></i></a>';
  cartHeaderAndFooter += '</li></ul></div></div></div></div></div>';
  
  var cartList = '<li class="wimix_area"><div class="product-details" style="width:60%;text-align:left;">item_name</div>';
	cartList += '<div class="product-details" style="width:40%;text-align:right;"><span>item_qty</span><span>x</span><span>item_price</span></div></li>';
	var noItems = '<li class="wimix_area"><div class="product-details" style="width:60%;text-align:left;">item_name</div>';
  noItems += '<div class="product-details" style="width:40%;text-align:right;"><span>item_qty</span><span>item_price</span></div></li>';
  
  $.ajax({
	  type: "POST",
	  url: "c.aspx?a=ajaxShoppingCart",
	  async: true,
	  success: function (msg) {
      eval('var data=' + msg);
      console.log(data)
			var dQtyTotal = data.dQtyTotal;
			var dOrderTotal = data.dOrderTotal;
			var dQtyTotalTax = data.dQtyTotalTax;			
			var dQtyTotalAmount = data.dQtyTotalAmount;				
			var dataList = data.dataList;		
      var spItem = "";
      if(dataList.length == 0)
			{
				var si = noItems;
				si = si.replace(/item_name/g, " Your cart is empty ");
				si = si.replace(/item_qty/g, "");
				si = si.replace(/item_price/g, "");
				spItem  += si;
			}
			for(var i=0; i<dataList.length; i++)
			{
				var name = dataList[i].name;
				var qty = dataList[i].qty;
				var price = dataList[i].price;
				var si = cartList;
				si = si.replace(/item_name/g, name);
				si = si.replace(/item_qty/g, qty);
				si = si.replace(/item_price/g, price);	
				spItem  += si;
			}
			if(dQtyTotal > 100){
				dQtyTotal = "99+"
			}
			cartHeaderAndFooter = cartHeaderAndFooter.replace(/new_list/g, spItem);
			cartHeaderAndFooter = cartHeaderAndFooter.replace(/qty_total/g, dQtyTotal);
			cartHeaderAndFooter = cartHeaderAndFooter.replace(/price_total/g, dOrderTotal);
			cartHeaderAndFooter = cartHeaderAndFooter.replace(/price_tax/g, dQtyTotalTax);
			cartHeaderAndFooter = cartHeaderAndFooter.replace(/price_amount/g, dQtyTotalAmount);
			$('#ajax_shopping_cart').append(cartHeaderAndFooter);
			$('#qty_badge').append(dQtyTotal);
			ajaxPrintCartOverFlow();
	  },
	});
}

function ajaxAddToCart() {
	var offset = $("#end").offset();
	var sh = $(window).scrollTop();
	$(window).resize(function() {
		sh = $(window).scrollTop();
		offset = $("#end").offset();
		console.log("sh1 =====",sh);
		console.log("resize1 =====",offset);
	});
	$('.add_to_cart').each(function(i,el){
		$(el).click(function(event){
			var e = event;
			console.log("sh2 =====",sh);
			console.log("resize2 =====",offset);
			console.log(e);
			var atc = $(this);
			var atc_img = atc.parent().parent().parent().parent().parent().parent().parent().find('.primary-img').attr('src');
			var flyer = $('<img class="u-flyer" src="'+ atc_img +'">');
			var atc_url = $(this).attr("data-href");
			console.log(atc_img);
			console.log(atc_url);
			$.ajax({
				type: "POST",
				url: atc_url,
				async: true,
				success: function (msg) {
					if(msg == "add to cart success"){
						flyer.fly({
							start: {
								left: e.clientX,
								top: e.clientY,
								width: 130,
								height: 100
							},
							end: {
								left: offset.left + 10,
								top: offset.top + 10 -sh,
								width: 0,
								height: 0
							},
							onEnd: function(){
								$("#msg").show().animate({width: '90px'}, 200).fadeOut(1000);
								this.destory();
								ajaxLoadItems();
								ajaxPrintCartOverFlow();
							}
						});
					}
				},
			});
		});
	})
}
function ajaxPrintCartOverFlow() {
	var itemHeightSum = 0;
	var fix = $('.new-list');                                     
	$('.new-list .wimix_area').each(function(){
		itemHeightSum += parseInt($(this).height());
	})
	if(itemHeightSum <= 450){
		fix.removeClass('cart-overflow');
	}
	else{
		fix.addClass('cart-overflow');
	}
}
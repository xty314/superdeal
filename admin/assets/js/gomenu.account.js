;(function () {
	

	'use strict';

	// Placeholder 
	var placeholderFunction = function() {
		$('input, textarea').placeholder({ customClass: 'my-placeholder' });
	}
	
	// Placeholder 
	var contentWayPoint = function() {
		var i = 0;
		$('.animate-box').waypoint( function( direction ) {

			if( direction === 'down' && !$(this.element).hasClass('animated-fast') ) {
				
				i++;

				$(this.element).addClass('item-animate');
				setTimeout(function(){

					$('body .animate-box.item-animate').each(function(k){
						var el = $(this);
						setTimeout( function () {
							var effect = el.data('animate-effect');
							if ( effect === 'fadeIn') {
								el.addClass('fadeIn animated-fast');
							} else if ( effect === 'fadeInLeft') {
								el.addClass('fadeInLeft animated-fast');
							} else if ( effect === 'fadeInRight') {
								el.addClass('fadeInRight animated-fast');
							} else {
								el.addClass('fadeInUp animated-fast');
							}

							el.removeClass('item-animate');
						},  k * 200, 'easeInOutExpo' );
					});
					
				}, 100);
				
			}

		} , { offset: '85%' } );
	};
	// On load
	$(function(){
		placeholderFunction();
		contentWayPoint();

	});

}());
//login
$(document).ready(function() {
    $('input.login').click(function (event) {
        event.preventDefault();//stop click
        if ($('#email').val() == "" || $('#pass').val() == "") {
            $(".alert-email .alert").html("Email or password can not be empty!").removeClass("alert-success").addClass("alert-danger");
            $(".alert-email").css("display","block");
            setTimeout(function () {
                $('.alert-email').css("display","none");
            }, 10000);
        }
        else {
            $.ajax({
                type: "POST",
                url: "login.aspx?t=j&a=login",
                data: "email=" + escape($('#email').val()) + "&pwd=" + escape($('#pass').val()),
                success: function (result) {
					console.log(result);
					eval('var info=' + result);
					var r = info.result;
					var m = info.msg;
                    if (r == "success") {
                        $(".alert-email .alert").html("Login success.").removeClass("alert-danger").addClass("alert-success");
                        $(".alert-email").css("display","block");
                        setTimeout(function () {
                            $('.alert-email').css("display","none");
                            $('#login').click(); //after login success 2's, click login
                        }, 2000);
                    }
                    if (r == "fail") {
                        $(".alert-email .alert").html(m).removeClass("alert-success").addClass("alert-danger");
                        $(".alert-email").css("display","block");
                        setTimeout(function () {
                            $('.alert-email').css("display","none");
                        }, 10000);
                    }
                },
                error: function (XMLHttpRequest, textStatus, thrownError) {
                }
            });
        }
    });
});
$(document).ready(function() {
	$('#email').blur(function () {
		var ev = $(this).val();
		if(ev.indexOf("@")==-1 || ev.indexOf(".")==-1){
			$(".alert-email .alert").html("Your email address is missing an \"@\" sign or a \".\".").removeClass("alert-success").addClass("alert-danger");
			$(".alert-email").css("display","block");
			setTimeout(function () {
				$('.alert-email').css("display","none");
			}, 10000);
		}
	});
	$('#pass').blur(function () {
		if($(this).val().length<4){
			$(".alert-email .alert").html("Your password must be at least 4 characters long.").removeClass("alert-success").addClass("alert-danger");
			$(".alert-email").css("display","block");
			setTimeout(function () {
				$('.alert-email').css("display","none");
			}, 10000);
		}
	});
});
//register
$(function () {
	$('#mailbox').blur(function (){mailBoxBlur();});
	$('#name').blur(function (){nameBlur()});
	$('#phone').blur(function (){phoneBlur()});
	$('#password').blur(function (){passwordBlur()});
	$('#re-password').blur(function (){rePasswordBlur()});

	function mailBoxBlur(){
		var ala = $(".alert-email .alert");
		var al = $(".alert-email");
		var i = $('#mailbox');
		if (i.val() == "") {
			ala.html("Please enter email.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else if (i.val().indexOf("@")==-1 || i.val().indexOf(".")==-1){
			ala.html("The email address is missing an \"@\" sign or a \".\".").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else {
			$.ajax({
				type: "POST",
				url: "login.aspx?t=j&a=reg",
				data: "email=" + escape(i.val()),
				success: function (msg) {
					if (msg == "success") {
						//ala.html("Available email�?).removeClass("alert-danger").addClass("alert-success");
						//al.css("display","block");
						i.removeClass("alert-r").addClass("alert-g");
						//setTimeout(function () {
						al.css("display","none");
						//}, 3000);
						return true;
					}
					else{
						ala.html("Occupied email.").removeClass("alert-success").addClass("alert-danger");
						al.css("display","block");
						i.removeClass("alert-g").addClass("alert-r");
						setTimeout(function () {
							al.css("display","none");
						}, 10000);
						return false;
					}
				},
				error: function (XMLHttpRequest, textStatus, thrownError) {
				}
			});
			return true;
		}
	}
	function nameBlur(){
		var ala = $(".alert-name .alert");
		var al = $(".alert-name");
		var i = $('#name');
		if (i.val() == "") {
			ala.html("Please enter name.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else if(i.val().length<4){
			ala.html("Your username must be at least 4 characters long.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else {
			//ala.html("Available name�?).removeClass("alert-danger").addClass("alert-success");
			//al.css("display","block");
			i.removeClass("alert-r").addClass("alert-g");
			//setTimeout(function () {
			al.css("display","none");
			//}, 3000);
			return true;
		}
	}
	$('#phone').keydown(function(event) { 
		var keyCode = event.which; 
		if ((keyCode >= 96 && keyCode <=105) || (keyCode >= 48 && keyCode <=57) || keyCode == 8 || keyCode == 9) 
			return true; 
		else 
			return false; 
	}).focus(function() { this.style.imeMode='disabled'; }); 
	function phoneBlur(){
		var ala = $(".alert-phone .alert");
		var al = $(".alert-phone");
		var i = $('#phone');
		if (i.val() == "") {
			ala.html("Please enter phone number.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else if(i.val().length<4){
			ala.html("The phone must be at least 4 characters long.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else {
			//ala.html("Available phone�?).removeClass("alert-danger").addClass("alert-success");
			//al.css("display","block");
			i.removeClass("alert-r").addClass("alert-g");
			//setTimeout(function () {
			al.css("display","none");
			//}, 3000);
			return true;
		}
	}
	function passwordBlur(){
		var ala = $(".alert-password .alert");
		var al = $(".alert-password");
		var i = $('#password');
		if (i.val() == "") {
			ala.html("Please enter password.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else if(i.val().length<4){
			ala.html("The password must be at least 4 characters long.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else {
			//ala.html("Available password�?).removeClass("alert-danger").addClass("alert-success");
			//al.css("display","block");
			i.removeClass("alert-r").addClass("alert-g");
			//setTimeout(function () {
			al.css("display","none");
			//}, 3000);
			return true;
		}
	}
	function rePasswordBlur(){
		var ala = $(".alert-re-password .alert");
		var al = $(".alert-re-password");
		var i = $('#re-password');
		if (i.val() == "") {
			ala.html("Please retype password.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else if(i.val() != $('#password').val()){
			ala.html("Retyped password is not the same, please retype.").removeClass("alert-success").addClass("alert-danger");
			al.css("display","block");
			i.removeClass("alert-g").addClass("alert-r");
			setTimeout(function () {
				al.css("display","none");
			}, 10000);
			return false;
		}
		else {
			//ala.html("Available password�?).removeClass("alert-danger").addClass("alert-success");
			//al.css("display","block");
			i.removeClass("alert-r").addClass("alert-g");
			//setTimeout(function () {
			al.css("display","none");
			//}, 3000);
			return true;
		}
	}
	$('#reg').click(function () {
		var ala = $(".alert-register .alert");
		var al = $(".alert-register");
		var i = $(this);
		if (nameBlur() == false || phoneBlur() == false || mailBoxBlur() == false || passwordBlur() == false || rePasswordBlur() == false ) {
			return false;
		}
		else {
			ala.html("Register success.").removeClass("alert-danger").addClass("alert-success");
			al.css("display","block");
			i.removeClass("alert-r").addClass("alert-g");
			setTimeout(function () {
				al.css("display","none");
				$("#register").click();
			}, 3000);
			return true;
		}
	});
});
//send password
$(function () {
            $('#send').click(function () {
                var ala = $(".alert-email .alert");
                var al = $(".alert-email");
                var i = $("#email");
                if (i.val() == "") {
                    ala.html("Please enter email.").removeClass("alert-success").addClass("alert-danger");
                    al.css("display","block");
                    i.removeClass("alert-g").addClass("alert-r");
                    setTimeout(function () {
                        al.css("display","none");
                    }, 10000);      
                }
                else {
                $.ajax({
                    type: "POST",
                    url: "login.aspx?t=j&a=reset",
                    data: "email=" + escape(i.val()),
                    success: function (msg) {
                        if (msg == "success") {
                            ala.html("Email sent successfully.").removeClass("alert-danger").addClass("alert-success");
                            al.css("display","block");
                            i.removeClass("alert-r").addClass("alert-g");
                            setTimeout(function () {
                            al.css("display","none");
                            ala.css("display","none");
                            }, 3000);
                        }
                        else{
                            ala.html(msg).removeClass("alert-success").addClass("alert-danger");
                            al.css("display","block");
                            i.removeClass("alert-g").addClass("alert-r");
                            setTimeout(function () {
                                al.css("display","none");
                            }, 10000);  
                        }
                    },
                    error: function (XMLHttpRequest, textStatus, thrownError) {
                    }
                });
                }
            });
        });
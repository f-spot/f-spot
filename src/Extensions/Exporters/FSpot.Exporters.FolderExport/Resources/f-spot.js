function setActiveStyleSheet(title) {
   var i, a, main;
   for(i=0; (a = document.getElementsByTagName("link")[i]); i++) {
     if(a.getAttribute("rel").indexOf("style") != -1
        && a.getAttribute("title")) {
       a.disabled = true;
       if(a.getAttribute("title") == title) a.disabled = false;
     }
   }
   if (title!="") {
      setCookie("theme", title);
   }
}

function getInactiveStyleSheet() {
  var i, a;
  for(i=0; (a = document.getElementsByTagName("link")[i]); i++) {
    if(a.getAttribute("rel").indexOf("style") != -1 && a.getAttribute("title") && a.disabled) return a.getAttribute("title");
  }
  return null;
}


function setCookie(name, value, expires, path, domain, secure)
{
    document.cookie= name + "=" + escape(value) +
        ((expires) ? "; expires=" + expires.toGMTString() : "") +
        ((path) ? "; path=" + path : "") +
        ((domain) ? "; domain=" + domain : "") +
        ((secure) ? "; secure" : "");
}

function getCookie(name)
{
    var dc = document.cookie;
    var prefix = name + "=";
    var begin = dc.indexOf("; " + prefix);
    if (begin == -1)
    {
        begin = dc.indexOf(prefix);
        if (begin != 0) return null;
    }
    else
    {
        begin += 2;
    }
    var end = document.cookie.indexOf(";", begin);
    if (end == -1)
    {
        end = dc.length;
    }
    return unescape(dc.substring(begin + prefix.length, end));
}

function deleteCookie(name, path, domain)
{
    if (getCookie(name))
    {
        document.cookie = name + "=" +
            ((path) ? "; path=" + path : "") +
            ((domain) ? "; domain=" + domain : "") +
            "; expires=Thu, 01-Jan-70 00:00:01 GMT";
    }
}


function checkForTheme() {
   var theme = getCookie('theme');

   //alert(theme);
   if (theme) {
      setActiveStyleSheet(theme);
   }
}

// to hide and show the styles
// inspired by www.wikipedia.org
function toggle_stylebox() {
    var stylebox = document.getElementById('stylebox');
    var showlink=document.getElementById('showlink');
    var hidelink=document.getElementById('hidelink');
    if(stylebox.style.display == 'none') {
	stylebox_was = stylebox.style.display;
	stylebox.style.display = '';
	hidelink.style.display='';
	showlink.style.display='none';
    } else {
	stylebox.style.display = stylebox_was;
	hidelink.style.display='none';
	showlink.style.display='';
    }
}
 
document.onkeyup = Navigate; // if key is pressed call function for navigation

function Navigate(key)
{
	var _Key = (window.event) ? event.keyCode : key.keyCode;
	switch(_Key) 
	{
		case 37: //arrow left
			window.location = nav(-1); break;
		case 39: //arrow right
			window.location = nav(+1); break;
	}
}

//calculate next file name 
function nav(direction)
{
	var regexp = new RegExp( "img-([0-9\.]*).html" ); 
	var result = regexp.exec( window.location.href );
	if ( result == null ) // redirect from index*.html to img-1.hml with any of key
		return "img-1.html";
	else
		var next = parseInt(result[1]) + parseInt(direction); //calculate next file number
	if( next == 0 || (next > result[1] && !checkobject('next'))) //if next page number is higher then current, check if exist id="next" on page, if next number for page is 0 or if js is called from other then page then img-*.html: redirect to index.html
		return "index.html";
	else // return next html page name for redirection
		return "img-" + next + ".html";
}

//check if object exist in webpage id="object"
function checkobject(object)
{
	if (document.getElementById(object) != null)
		return true;
	else
		return false;
}


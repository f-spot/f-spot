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

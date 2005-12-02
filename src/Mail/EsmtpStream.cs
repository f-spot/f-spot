//
// System.Web.Mail.EsmtpStream.cs
//
// Author(s):
//   Per Arneng <pt99par@student.bth.se>
//
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Security.Cryptography;

namespace FSpot.Mail {

    internal class EsmtpStream {
	
	protected Stream stream;
	protected Encoding encoding;
	protected SmtpResponse lastResponse;
	protected string command = "";

	public EsmtpStream( Stream stream ) {
	    this.stream = stream;
	    encoding = new ASCIIEncoding();
	}
	
	public Stream Stream {
	    get { return stream; }
	}
	
	public SmtpResponse LastResponse {
	    get { return lastResponse; }
	}
	
	public void WriteRset() {
	    command = "RSET";
	    WriteLine( command );
	    ReadResponse();
	    CheckForStatusCode( 250 );
	
	}
	
	public void WriteHelo( string hostName ) { 
	    command = "EHLO " + hostName;
	    WriteLine( command );
	    ReadResponse();
	    CheckForStatusCode( 250 );
	    
	}
	
	public void WriteMailFrom( string from ) {
	    command = "MAIL FROM: <" + from + ">";
	    WriteLine( command );
	    ReadResponse();
	    CheckForStatusCode( 250 );
	    
	}
	
	public void WriteRcptTo( string to ) {
	    command = "RCPT TO: <" + to + ">";  
	    WriteLine( command );
	    ReadResponse();
	    CheckForStatusCode( 250 );
	   
	}
	
	    public void WriteAuth (string username, string password)
	    {
		    command = "AUTH LOGIN";
		    WriteLine (command);
		    ReadResponse ();
		    CheckForStatusCode (334);

		    Console.WriteLine (lastResponse.RawResponse);
		    Console.WriteLine (Encoding.UTF8.GetString (Convert.FromBase64String (lastResponse.RawResponse.Split (' ') [1])));

		    command = Convert.ToBase64String (Encoding.UTF8.GetBytes (username));
		    WriteLine (command);
		    ReadResponse ();
		    CheckForStatusCode (334);

		    Console.WriteLine (lastResponse.RawResponse);
		    Console.WriteLine (Encoding.UTF8.GetString (Convert.FromBase64String (lastResponse.RawResponse.Split (' ') [1])));


		    command = Convert.ToBase64String (Encoding.UTF8.GetBytes (password));
		    WriteLine (command);
		    ReadResponse ();
		    CheckForStatusCode (235);
	    }


	public void WriteData() {
	    command = "DATA";
	    WriteLine( command );
	    ReadResponse();
	    CheckForStatusCode( 354 );
	
	}
	
	public void WriteQuit() {
	    command = "QUIT";
	    WriteLine( command );
	    try {
		    ReadResponse();
		    CheckForStatusCode( 221 );
	    } catch (System.Exception e) {
		    // ignore errors on quit
	    }
	}
		
	public void WriteBoundary( string boundary ) {
	
	    WriteLine( "\r\n--{0}" , boundary );
	
	}
	
	public void WriteFinalBoundary( string boundary ) {
	
	    WriteLine( "\r\n--{0}--" , boundary );
	
	}
	
	// single dot by itself
	public void WriteDataEndTag() {
	    command = "\r\n.";
	    WriteLine( command );
	    ReadResponse();
	    CheckForStatusCode( 250 );
	
	}
	
	public void WriteHeader( MailHeader header ) {
	    // write the headers
	    foreach( string key in header.Data.AllKeys )
		WriteLine( "{0}: {1}" , key , header.Data[ key ] );
	    
	    // write the header end tag
	    WriteLine( "" );
	}
	
	public void CheckForStatusCode( int statusCode ) {
	    
	    if( LastResponse.StatusCode != statusCode ) {
		
		string msg = "" + 
		    "Server reponse: '" + lastResponse.RawResponse + "';" +
		    "Status code: '" +  lastResponse.StatusCode + "';" + 
		    "Expected status code: '" + statusCode + "';" + 
		    "Last command: '" + command + "'";
		
		throw new SmtpException( msg ); 
					
	    }
	}
	
	
	// write buffer's bytes to the stream
	public void WriteBytes( byte[] buffer ) {
	    stream.Write( buffer , 0 , buffer.Length );
	}
	
	// writes a formatted line to the server
	public void WriteLine( string format ,  params object[] args ) {
	    WriteLine( String.Format( format , args ) );
	}
	
	// writes a line to the server
	public void WriteLine( string line ) {
	    byte[] buffer = encoding.GetBytes( line + "\r\n" );
	    
	    stream.Write( buffer , 0 , buffer.Length );
	
	    #if DEBUG 
	      DebugPrint( line );
            #endif
	}
	
	// read a line from the server
	public void ReadResponse( ) {
	    string line = null;
	    
	    byte[] buffer = new byte[ 4096 ];
	    
	    int readLength = stream.Read( buffer , 0 , buffer.Length );
	    
	    if( readLength > 0 ) { 
	    
		line = encoding.GetString( buffer , 0 , readLength );
		
		line = line.TrimEnd( new Char[] { '\r' , '\n' , ' ' } );
			
	    }
	   
	    // parse the line to the lastResponse object
	    lastResponse = SmtpResponse.Parse( line );
	   
	    #if DEBUG
	      DebugPrint( line );
	    #endif
	}
	
    #if DEBUG
	/// debug printing 
	private void DebugPrint( string line ) {
	    Console.WriteLine( "smtp: {0}" , line );
	}
    #endif

    }


}

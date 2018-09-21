using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
    
/*
* zahmed
* Date 23 Jan 2004
* Socks 5 RFC is available at http://www.faqs.org/rfcs/rfc1928.html.
*/
namespace LMKR
{
public class ConnectionException:ApplicationException
{
public ConnectionException(string message)
	:base(message)
{
}
}

/// <summary>
/// Provides sock5 functionality to clients (Connect only).
/// </summary>
public class SocksProxy
{

private SocksProxy(){} 

#region ErrorMessages
private static string[] errorMsgs=	{
										"Operation completed successfully.",
										"General SOCKS server failure.",
										"Connection not allowed by ruleset.",
										"Network unreachable.",
										"Host unreachable.",
										"Connection refused.",
										"TTL expired.",
										"Command not supported.",
										"Address type not supported.",
										"Unknown error."
									};
#endregion


public static Socket ConnectToSocks5Proxy(string proxyAdress, ushort proxyPort, string destAddress, ushort destPort,
	string userName, string password)
{
	IPAddress destIP = null;
	IPAddress proxyIP = null;
	byte[] request = new byte[257];
	byte[] response = new byte[257];
	ushort nIndex;

	try
	{
		proxyIP =  IPAddress.Parse(proxyAdress);
	}
	catch(FormatException)
	{	// get the IP address
		proxyIP = Dns.GetHostByAddress(proxyAdress).AddressList[0];
	}

	// Parse destAddress (assume it in string dotted format "212.116.65.112" )
	try
	{
		destIP = IPAddress.Parse(destAddress);
	}
	catch(FormatException)
	{
		// wrong assumption its in domain name format "www.microsoft.com"
	}

	IPEndPoint proxyEndPoint = new IPEndPoint(proxyIP,proxyPort);

	// open a TCP connection to SOCKS server...
	Socket s = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
	s.Connect(proxyEndPoint);	

	nIndex = 0;
	request[nIndex++]=0x05; // Version 5.
	request[nIndex++]=0x01; // 2 Authentication methods are in packet...
	request[nIndex++]=0x00; // NO AUTHENTICATION REQUIRED
	//request[nIndex++]=0x02; // USERNAME/PASSWORD
	// Send the authentication negotiation request...
	s.Send(request,nIndex,SocketFlags.None);

	// Receive 2 byte response...
	int nGot = s.Receive(response,2,SocketFlags.None);	
	if (nGot!=2)
		throw new ConnectionException("Bad response received from proxy server.");

	if (response[1]==0xFF)
	{	// No authentication method was accepted close the socket.
		s.Close();
		throw new ConnectionException("None of the authentication method was accepted by proxy server.");
	}
    
	byte[] rawBytes;

	if (/*response[1]==0x02*/false)
	{//Username/Password Authentication protocol
		nIndex = 0;
		request[nIndex++]=0x05; // Version 5.
    	
		// add user name
		request[nIndex++]=(byte)userName.Length;
		rawBytes = Encoding.Default.GetBytes(userName);
		rawBytes.CopyTo(request,nIndex);
		nIndex+=(ushort)rawBytes.Length;

		// add password
		request[nIndex++]=(byte)password.Length;
		rawBytes = Encoding.Default.GetBytes(password);
		rawBytes.CopyTo(request,nIndex);
		nIndex+=(ushort)rawBytes.Length;

		// Send the Username/Password request
		s.Send(request,nIndex,SocketFlags.None);
		// Receive 2 byte response...
		nGot = s.Receive(response,2,SocketFlags.None);	
		if (nGot!=2)
			throw new ConnectionException("Bad response received from proxy server.");
		if (response[1] != 0x00)
			throw new ConnectionException("Bad Usernaem/Password.");
	}
	// This version only supports connect command. 
	// UDP and Bind are not supported.

	// Send connect request now...
	nIndex = 0;
	request[nIndex++]=0x05;	// version 5.
	request[nIndex++]=0x01;	// command = connect.
	request[nIndex++]=0x00;	// Reserve = must be 0x00

	if (destIP != null)
	{// Destination adress in an IP.
		switch(destIP.AddressFamily)
		{
			case AddressFamily.InterNetwork:
				// Address is IPV4 format
				request[nIndex++]=0x01;
				rawBytes = destIP.GetAddressBytes();
				rawBytes.CopyTo(request,nIndex);
				nIndex+=(ushort)rawBytes.Length;
				break;
			case AddressFamily.InterNetworkV6:
				// Address is IPV6 format
				request[nIndex++]=0x04;
				rawBytes = destIP.GetAddressBytes();
				rawBytes.CopyTo(request,nIndex);
				nIndex+=(ushort)rawBytes.Length;
				break;
		}
	}
	else
	{// Dest. address is domain name.
		request[nIndex++]=0x03;	// Address is full-qualified domain name.
		request[nIndex++]=Convert.ToByte(destAddress.Length); // length of address.
		rawBytes = Encoding.Default.GetBytes(destAddress);
		rawBytes.CopyTo(request,nIndex);
		nIndex+=(ushort)rawBytes.Length;
	}

	// using big-edian byte order
	byte[] portBytes = BitConverter.GetBytes(destPort);
	for (int i=portBytes.Length-1;i>=0;i--)
		request[nIndex++]=portBytes[i];
    
	// send connect request.
	s.Send(request,nIndex,SocketFlags.None);
	s.Receive(response);	// Get variable length response...
	if (response[1]!=0x00)
		throw new ConnectionException(errorMsgs[response[1]]);
	// Success Connected...
	return s;
}
}
}





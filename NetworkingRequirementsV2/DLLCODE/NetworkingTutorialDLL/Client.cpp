#include "pch.h"
#include "Client.h"

void Client::Init(const char* p_IP, const int p_port, const char* name, bool wsaisinit)
{
	WSAData data;
	WORD version = MAKEWORD(2, 2);

	if (!wsaisinit)
	{
		if (WSAStartup(version, &data) != 0)
		{
			std::cout << "Error initializing winsock\n";
			return;
		}
	}

	std::cout << "Client Initialized\n";

	IP = p_IP;
	port = p_port;

	clientName = name;

	ConnectToServer();
}

void Client::ConnectToServer()
{
	serverAddr.sin_addr.S_un.S_addr = INADDR_BROADCAST;
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_port = htons(port);
	inet_pton(AF_INET, IP, &serverAddr.sin_addr);

	sock = socket(AF_INET, SOCK_DGRAM, 0);
	if (sock == INVALID_SOCKET)
	{
		std::cout << "Invalid Socket\n";
		return;
	}

	StartListen();

	SendPacketToServer(clientName);
}

void Client::SendPacketToServer(const char* msg)
{
	std::cout << "SENDING: " << msg << std::endl;

	if (sendto(sock, msg, std::string(msg).length(), 0, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
	{
		std::cout << "ERR: Could not send message to server.\n";
		return;
	}
}

void Client::Cleanup()
{
	system("CLS");
	listening = false;
	closesocket(sock);
}

void Client::StartListen()
{
	listening = true;
	std::thread(&Client::RecvLoop, this).detach();
}

void Client::RecvLoop()
{
	int sizeofserver = sizeof(serverAddr);
	char buf[256];

	while (listening)
	{
		if (!listening)
			return;

		ZeroMemory(buf, 256);

		int numBytesReceived = recvfrom(sock, buf, 256, 0, (sockaddr*)&serverAddr, &sizeofserver);

		if (numBytesReceived > 0)
		{
			ProcessMessage(buf);
		}
	}
}

void Client::ProcessMessage(const char* msg)
{
	std::cout << "Client received: " << msg << std::endl;
	funcs.MsgReceived(msg);
}

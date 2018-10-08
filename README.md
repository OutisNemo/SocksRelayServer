# SocksRelayServer

## About the project
[![Build Status](https://travis-ci.org/OutisNemo/SocksRelayServer.svg?branch=master)](https://travis-ci.org/OutisNemo/SocksRelayServer) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/aaa423bd8b494a5eb4af2bd143800c0c)](https://app.codacy.com/app/brnbs/SocksRelayServer?utm_source=github.com&utm_medium=referral&utm_content=OutisNemo/SocksRelayServer&utm_campaign=Badge_Grade_Dashboard)

A simple SOCKS v4a proxy server written in C# which forwards all traffic to a SOCKS v5 server. The proxy server does not support authentication however it can connect to a SOCKS v5 server using username and password. UDP and Bind commands are not supported. It also provides an interface for custom DNS resolving and a default DNS resolver as well. It can also use the upstream proxy for resolving the hostnames.

## How to install
You can easily install this package to your project using NuGet.

See the [NuGet page](https://www.nuget.org/packages/OutisNemo.SocksRelayServer/)

## How to use
You can find detailed usage examples in the `SocksRelayServer/Tests` project.

## How to write a custom DNS resolver
All you need to do is implement the `IDnsResolver` interface and pass your implementation to the `SocksRelayServer` instance using it's `DnsResolver` property. You can see a working example in the `SocksRelayServer/Tests` project.

## License

See the [LICENCE file](LICENCE.md) in this repository.

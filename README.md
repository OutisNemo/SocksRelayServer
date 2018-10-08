# SocksRelayServer

## About the project
[![Build Status](https://travis-ci.org/OutisNemo/SocksRelayServer.svg?branch=master)](https://travis-ci.org/OutisNemo/SocksRelayServer) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/aaa423bd8b494a5eb4af2bd143800c0c)](https://app.codacy.com/app/brnbs/SocksRelayServer?utm_source=github.com&utm_medium=referral&utm_content=OutisNemo/SocksRelayServer&utm_campaign=Badge_Grade_Dashboard)

A simple SOCKS v4a proxy server written in C# which forwards all traffic to a SOCKS v5 server. The proxy server does not support authentication however it can connect to a SOCKS v5 server using username and password. UDP and Bind commands are not supported. It also provides an interface for custom DNS resolving and a default DNS resolver as well. It can also use the upstream proxy for resolving the hostnames.

## How to install
You can easily install this package to your project using NuGet.

## How to use
You can find detailed usage examples in the `SocksRelayServer/Tests` project.

## How to write a custom DNS resolver
All you need to do is implement the `IDnsResolver` interface and pass your implementation to the `SocksRelayServer` instance using it's `DnsResolver` property. You can see a working example in the `SocksRelayServer/Tests` project.

## License

> MIT License
>
> Copyright (c) 2018 Outis Nemo Ltd.
>
> Permission is hereby granted, free of charge, to any person obtaining
> a copy of this software and associated documentation files (the
> "Software"), to deal in the Software without restriction, including
> without limitation the rights to use, copy, modify, merge, publish,
> distribute, sublicense, and/or sell copies of the Software, and to
> permit persons to whom the Software is furnished to do so, subject to
> the following conditions:
>
> The above copyright notice and this permission notice shall be
> included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
> EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
> MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
> IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
> CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
> TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
> SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

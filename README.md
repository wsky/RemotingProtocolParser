Remoting Protocol Parser
======================

Help communicating with .NET Remoting via MS build-in Binary or SOAP.

You can refer it to build your CrossPlatform-ServiceFramework/RPC written by other language.

Protocal parser logic:

```c#
//Read
//1. Read Preamble, will be ".Net"
//2. MajorVersion, will be 1
//3. MinorVersion, will be 0
//4. Operation, will be 5 (request,onewayrequest...)
//5. TcpContentDelimiter and ContentLength
//6. Read Headers(ITransportHeaders)
//7. RequestStream/Message

//Write
//same as read

```


## Upcoming

- HTTP Parser
- Buffer Improvement
- .Net Remoting MockServer for testing
- Parser of NodeJS
- Parser of Java

## Not Support

- Chunked (I think it useless for RPC)
- SOAP via TCP/HTTP (you can use other standard lib or impl yourself)

## Reference

[[MS-NRTP]: .NET Remoting: Core Protocol](http://msdn.microsoft.com/en-us/library/cc237297(v=prot.20).aspx)

[.NET Remoting](https://github.com/wsky/System.Runtime.Remoting)

[MONO Remoting MessageIO](https://github.com/mono/mono/blob/master/mcs/class/System.Runtime.Remoting/System.Runtime.Remoting.Channels.Tcp/TcpMessageIO.cs)

[A Note Of Remoting Protocol](https://github.com/ali-ent/apploader/issues/4)

## License

(The MIT License)

Copyright (C) 2012 wsky (wskyhx at gmail.com) and other contributors

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
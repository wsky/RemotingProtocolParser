/*
    (The MIT License)

    Copyright (C) 2012 wsky (wskyhx at gmail.com) and other contributors

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

var parser = require('./lib/remotingProtocolParser'),
	tcpWriter = parser.tcpWriter,
	tcpReader = parser.tcpReader,
	client = new require('net').Socket(),
	HOST = 'localhost';
	PORT = 8080;
	ENCODING = 'ascii';

client.connect(PORT, HOST, function() {
	//send remoting request
	var msg = new Buffer('0123456789', ENCODING);
	var w = tcpWriter(client);
	//Preamble
	w.writePreamble();
	//MajorVersion
	w.writeMajorVersion();
	//MinorVersion
	w.writeMinorVersion();
	//Operation
	w.writeOperation(0);
	//ContentDelimiter
	w.writeContentDelimiter();
	//ContentLength
	w.writeContentLength(msg.length);
	//Headers
	w.writeHeaders();
	//Content
	w.writeContent(msg);
});

client.on('data', function(data) {
	//read remoting response
	var r = tcpReader(data);
	console.log('---- received ----');
	//Preamble
	console.log('Preamble: %s', r.readPreamble());
	//MajorVersion
	console.log('MajorVersion: %s', r.readMajorVersion());
	//MinorVersion
	console.log('MinorVersion: %s', r.readMinorVersion());
	//Operation
	console.log('Operation: %s', r.readOperation());
	//ContentDelimiter
	console.log('ContentDelimiter: %s', r.readContentDelimiter());
	//ContentLength
	console.log('ContentLength: %s', r.readContentLength());
	//Headers
	console.log('---- Headers ----');
	//Content
	console.log('---- Content ----');
	console.log(r.readContent());

	client.end();
});

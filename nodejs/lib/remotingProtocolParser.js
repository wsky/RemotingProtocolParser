/*
    (The MIT License)

    Copyright (C) 2012 wsky (wskyhx at gmail.com) and other contributors

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

module.exports.tcpWriter = function(socket) {
	var w = {};

	w.writePreamble = function() {
		//TODO:buffer management&pool
		socket.write(new Buffer('.NET', ENCODING));
	};
	w.writeMajorVersion = function() { 
		socket.write(new Buffer([1]));
	};
	w.writeMinorVersion = function() { 
		socket.write(new Buffer([0]));
	};
	w.writeOperation = function(opr) {
		writeUInt16(opr);
	};
	w.writeContentDelimiter = function() { 
		writeUInt16(0);
	};
	w.writeContentLength = function(length) {
		writeInt32(length);
	};
	w.writeHeaders = function(headers) {
		//end of header
		writeUInt16(0);
	};
	//TODO:write json message matching server-side message sink
	w.writeContent = function(v) {
		socket.write(v);
	};

	function writeUInt16(v) {
		var buffer = new Buffer(2);
		buffer.writeUInt16LE(v, 0);
		socket.write(buffer);
	}
	function writeInt32(v) {
		var buffer = new Buffer(4);
		buffer.writeInt32LE(v, 0);
		socket.write(buffer);
	}

	return w;
}

module.exports.tcpReader = function(buffer) {
	var r = {};
	r.position = 0;
	r.contentLength = -1;
	r.readPreamble = function() {
		r.position = 3;
		return buffer.toString(ENCODING, 0, 4); 
	};
	r.readMajorVersion = function() { 
		return buffer[r.position = 4];
	};
	r.readMinorVersion = function() { 
		return buffer[r.position = 5];
	};
	r.readOperation = function() {
		r.position = 7;
		return buffer.readUInt16LE(6);
	};
	r.readContentDelimiter = function() { 
		r.position = 9;
		return buffer.readUInt16LE(8);
	};
	r.readContentLength = function() {
		r.position = 13;
		return r.contentLength = buffer.readInt32LE(10);
	};
	r.readHeaders = function() {

	};
	r.readContent = function() { 
		return buffer.toString(ENCODING, 14);
	};
	return r;
}
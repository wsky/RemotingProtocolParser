package remoting.protocol.tcp;

import static org.junit.Assert.*;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.HashMap;

import org.junit.Test;

import remoting.protocol.NotSupportedException;

public class TcpProtocolTest {

	@Test
	public void write_read_contentLength_test() throws NotSupportedException {
		write_read_contentLength_test(10);
		write_read_contentLength_test(100);
		write_read_contentLength_test(1000);
		write_read_contentLength_test(10000);
		write_read_contentLength_test(100000);

		byte b=(byte) 160;
		System.out.println(b);
		
		//can support .net(c#) usignedbyte
		ByteBuffer buffer = ByteBuffer.wrap(new byte[] { (byte) 160, (byte) 134, 1, 0 });
		buffer.order(ByteOrder.LITTLE_ENDIAN);
		assertEquals(100000, buffer.getInt());

		buffer = ByteBuffer.wrap(new byte[] { -96, -122, 1, 0 });
		buffer.order(ByteOrder.LITTLE_ENDIAN);
		assertEquals(100000, buffer.getInt());
	}

	@Test
	public void parser_test() throws NotSupportedException {
		ByteBuffer buffer = ByteBuffer.allocate(1024);
		HashMap<String, Object> transportHeaders = new HashMap<String, Object>();
		transportHeaders.put("key", "key");
		byte[] data = new byte[] { 'h', 'i' };
		int contentLength = 2;

		TcpProtocolHandle handle = new TcpProtocolHandle(buffer);
		handle.WritePreamble();
		handle.WriteMajorVersion();
		handle.WriteMinorVersion();
		handle.WriteOperation(TcpOperations.Request);
		handle.WriteContentDelimiter(TcpContentDelimiter.ContentLength);
		handle.WriteContentLength(contentLength);
		handle.WriteTransportHeaders(transportHeaders);
		handle.WriteContent(data);

		buffer.flip();
		assertEquals(".NET", handle.ReadPreamble());
		assertEquals(1, handle.ReadMajorVersion());
		assertEquals(0, handle.ReadMinorVersion());
		assertEquals(TcpOperations.Request, handle.ReadOperation());
		assertEquals(TcpContentDelimiter.ContentLength, handle.ReadContentDelimiter());
		assertEquals(contentLength, handle.ReadContentLength());
		assertEquals("key", handle.ReadTransportHeaders().get("key"));
		assertEquals("hi", new String(handle.ReadContent()));
	}

	@Test
	public void WriteRequestAndReadResponse() {

	}

	@Test
	public void ReadRequestAndWriteResponse() {

	}

	private void write_read_contentLength_test(int length) {
		ByteBuffer buffer = ByteBuffer.allocate(1024);
		TcpProtocolHandle handle = new TcpProtocolHandle(buffer);
		handle.WriteContentLength(length);
		buffer.position(0);
		for (int i = 0; i < 4; i++)
			System.out.print(buffer.array()[i] + " ");
		System.out.println();
		assertEquals(length, handle.ReadContentLength());
	}
}

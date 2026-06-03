using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManager.Services;

public class RconService : IRconService
{
	private const int AuthPacketType = 3;

	private const int CommandPacketType = 2;

	private TcpClient? _client;

	private NetworkStream? _stream;

	private int _packetId = 100;

	public bool IsConnected => _client?.Connected ?? false;

	public async Task ConnectAsync(string host, int port, string password)
	{
		if (IsConnected)
		{
			await DisconnectAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_client = new TcpClient();
		try
		{
			await _client.ConnectAsync(host, port).ConfigureAwait(continueOnCapturedContext: false);
			_stream = _client.GetStream();
			await SendPacketAsync(++_packetId, AuthPacketType, password ?? string.Empty).ConfigureAwait(false);
			int authPacketId = _packetId;
			for (int i = 0; i < 3; i++)
			{
				RconPacket authResponse = await ReadPacketAsync(TimeSpan.FromSeconds(5.0)).ConfigureAwait(false);
				if (authResponse.Id == -1)
				{
					await DisconnectAsync().ConfigureAwait(false);
					throw new InvalidOperationException("RCON authentication failed. Check the server admin password.");
				}
				if (authResponse.Id == authPacketId && authResponse.Type == CommandPacketType)
				{
					return;
				}
			}
			throw new InvalidOperationException("RCON authentication did not complete. The server may still be booting.");
		}
		catch
		{
			await DisconnectAsync().ConfigureAwait(false);
			throw;
		}
	}

	public async Task DisconnectAsync()
	{
		if (_stream != null)
		{
			await _stream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
			_stream.Dispose();
			_stream = null;
		}
		_client?.Close();
		_client = null;
	}

	public async Task<string> SendCommandAsync(string command)
	{
		if (_stream == null || _client == null || !_client.Connected)
		{
			throw new InvalidOperationException("RCON client is not connected.");
		}
		int commandId = ++_packetId;
		await SendPacketAsync(commandId, CommandPacketType, command).ConfigureAwait(false);
		RconPacket response = await ReadPacketAsync(TimeSpan.FromSeconds(5.0)).ConfigureAwait(false);
		return response.Body.Trim();
	}

	private async Task SendPacketAsync(int id, int type, string body)
	{
		if (_stream == null)
		{
			throw new InvalidOperationException("RCON client is not connected.");
		}
		byte[] bodyBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
		int size = 4 + 4 + bodyBytes.Length + 2;
		byte[] packet = new byte[4 + size];
		WriteInt32(packet, 0, size);
		WriteInt32(packet, 4, id);
		WriteInt32(packet, 8, type);
		Buffer.BlockCopy(bodyBytes, 0, packet, 12, bodyBytes.Length);
		await _stream.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);
		await _stream.FlushAsync().ConfigureAwait(false);
	}

	private async Task<RconPacket> ReadPacketAsync(TimeSpan timeout)
	{
		if (_stream == null)
		{
			throw new InvalidOperationException("RCON client is not connected.");
		}
		using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
		byte[] sizeBytes = await ReadExactAsync(4, timeoutSource.Token).ConfigureAwait(false);
		int size = BitConverter.ToInt32(sizeBytes, 0);
		if (size < 10 || size > 1048576)
		{
			throw new InvalidDataException("Invalid RCON packet size.");
		}
		byte[] packetBytes = await ReadExactAsync(size, timeoutSource.Token).ConfigureAwait(false);
		int id = BitConverter.ToInt32(packetBytes, 0);
		int type = BitConverter.ToInt32(packetBytes, 4);
		string body = Encoding.UTF8.GetString(packetBytes, 8, Math.Max(0, size - 10));
		return new RconPacket(id, type, body);
	}

	private async Task<byte[]> ReadExactAsync(int length, CancellationToken cancellationToken)
	{
		if (_stream == null)
		{
			throw new InvalidOperationException("RCON client is not connected.");
		}
		byte[] buffer = new byte[length];
		int offset = 0;
		while (offset < length)
		{
			int read = await _stream.ReadAsync(buffer, offset, length - offset, cancellationToken).ConfigureAwait(false);
			if (read == 0)
			{
				throw new IOException("RCON connection closed by server.");
			}
			offset += read;
		}
		return buffer;
	}

	private static void WriteInt32(byte[] buffer, int offset, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
	}

	private readonly struct RconPacket
	{
		public int Id { get; }

		public int Type { get; }

		public string Body { get; }

		public RconPacket(int id, int type, string body)
		{
			Id = id;
			Type = type;
			Body = body;
		}
	}
}

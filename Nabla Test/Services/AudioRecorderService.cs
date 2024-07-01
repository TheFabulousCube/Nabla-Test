using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

namespace Nabla_Test.Services;

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

public class AmendAudioRecorderService
{
    private readonly IAudioManager audioManager;
    private IAudioRecorder audioRecorder;
    private ClientWebSocket webSocket;
    private readonly string streamId = "doctor_stream"; // replace with your stream_id
    private int seqId = 0;
    private const int chunkDuration = 100; // in milliseconds

    public event EventHandler<string> TranscriptionReceived;

    public AmendAudioRecorderService(IAudioManager audioManager)
    {
        this.audioManager = audioManager;
        this.audioRecorder = audioManager.CreateRecorder();
    }

    public async Task StartRecording()
    {
        if (!audioRecorder.CanRecordAudio)
        {
            throw new InvalidOperationException("Recording not supported on this device.");
        }

        await InitializeWebSocket();
        await audioRecorder.StartAsync();

        // Start a task to read audio data in chunks and send it over WebSocket
        Task.Run(() => ReadAudioDataInChunks());
    }

    public async Task StopRecording()
    {
        if (audioRecorder.IsRecording)
        {
            await audioRecorder.StopAsync();
        }

        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping recording", CancellationToken.None);
        }
    }

    private async Task InitializeWebSocket()
    {
        webSocket = new ClientWebSocket();
        webSocket.Options.SetRequestHeader("Authorization", "Bearer YOUR_SERVER_API_KEY");
        webSocket.Options.AddSubProtocol("copilot-listen-protocol");
        await webSocket.ConnectAsync(new Uri("wss://api.nabla.com/transcription"), CancellationToken.None);
        ReceiveTranscriptions();
    }

    private async Task ReadAudioDataInChunks()
    {
        var buffer = new byte[4096];

        using (var memoryStream = new MemoryStream())
        {
            while (audioRecorder.IsRecording)
            {
                int bytesRead = await ReadAudioChunk(memoryStream);
                if (bytesRead > 0)
                {
                    byte[] chunk = new byte[bytesRead];
                    Array.Copy(memoryStream.GetBuffer(), chunk, bytesRead);

                    await SendAudioChunk(chunk);
                    await Task.Delay(chunkDuration); // wait for the chunk duration before sending the next chunk
                }
            }
        }
    }

    private async Task<int> ReadAudioChunk(MemoryStream memoryStream)
    {
        var buffer = new byte[4096];
        int bytesRead = 0;

        while (memoryStream.Length < chunkDuration * 10) // read until we have enough data for one chunk
        {
            bytesRead = await audioRecorder.StartAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;
            memoryStream.Write(buffer, 0, bytesRead);
        }

        return (int)memoryStream.Length;
    }

    private async Task SendAudioChunk(byte[] chunk)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var base64Chunk = Convert.ToBase64String(chunk);
            var payload = new
            {
                @object = "audio_chunk",
                payload = base64Chunk,
                stream_id = streamId,
                seq_id = seqId++
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task ReceiveTranscriptions()
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var transcription = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            TranscriptionReceived?.Invoke(this, transcription);
        }
    }
}



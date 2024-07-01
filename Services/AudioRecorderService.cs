using System;
using System.Net.WebSockets;
//using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Plugin.Maui.Audio;

namespace Nabla_Test.Services;


public class AudioRecorderService
{
    private IAudioManager audioManager;
    private IAudioPlayer audioPlayer;

    public event EventHandler<byte[]> AudioChunkAvailable;

    public AudioRecorderService(IAudioManager audioManager)
    {
        this.audioManager = audioManager;
    }

    public async Task StartRecording()
    {
        // Implement recording logic here and call OnAudioChunkAvailable whenever a chunk is ready
    }

    public void StopRecording()
    {
        // Implement stop recording logic here
    }

    protected virtual void OnAudioChunkAvailable(byte[] chunk)
    {
        AudioChunkAvailable?.Invoke(this, chunk);
    }
}



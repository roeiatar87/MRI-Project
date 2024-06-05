using NAudio.Wave;
using System;
using System.Threading.Tasks;

public class MicrophoneListener
{
    private WaveInEvent waveSource;
    private BufferedWaveProvider bufferedWaveProvider;
    private WaveOutEvent waveOut;

    // Adjust the buffer size as needed
    private const int BufferSizeInMilliseconds = 200; // Example buffer size in milliseconds

    public MicrophoneListener()
    {
        waveSource = new WaveInEvent();
        waveSource.WaveFormat = new WaveFormat(44100,16, 1); // 44.1 kHz mono
        waveSource.DataAvailable += WaveSource_DataAvailable;
        bufferedWaveProvider = new BufferedWaveProvider(waveSource.WaveFormat);
        bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(BufferSizeInMilliseconds);
        waveOut = new WaveOutEvent();
    }

    public void StartListening()
    {
        waveOut.Init(bufferedWaveProvider); // Initialize WaveOutEvent instance
        waveSource.StartRecording();

        // Start audio playback asynchronously
        Task.Run(() => StartPlayback());
    }

    private void StartPlayback()
    {
        waveOut.Play();
    }

    public void StopListening()
    {
        bufferedWaveProvider.ClearBuffer();
        waveSource.StopRecording();
        waveOut.Stop();
        waveOut.Dispose();
       
    }

    private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
    {
        // Process audio data asynchronously
        Task.Run(() => ProcessAudioData(e.Buffer, e.BytesRecorded));
    }

    private void ProcessAudioData(byte[] buffer, int bytesRecorded)
    {
        try
        {
            if (bufferedWaveProvider.BufferedDuration < bufferedWaveProvider.BufferDuration)
            {
                bufferedWaveProvider.AddSamples(buffer, 0, bytesRecorded);
            }
            else
            {
  
                Console.WriteLine("Buffer full. Discarding incoming samples.");
            }
        }
        catch (Exception)
        {

           
        }

     
    }
}

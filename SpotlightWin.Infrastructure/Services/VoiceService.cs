using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.Wave;

namespace SpotlightWin.Infrastructure.Services
{
    public class VoiceService
    {
        private WaveInEvent? _waveIn;
        private MemoryStream? _audioStream;
        private WaveFileWriter? _writer;
        private bool _isRecording = false;

        public bool IsRecording => _isRecording;

        public void StartRecording()
        {
            if (_isRecording) return;

            _audioStream = new MemoryStream();
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz Mono ideal for Whisper
            };

            _writer = new WaveFileWriter(_audioStream, _waveIn.WaveFormat);

            _waveIn.DataAvailable += (s, e) =>
            {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
            };

            _waveIn.StartRecording();
            _isRecording = true;
        }

        public async Task<string> StopRecordingAndTranscribeAsync(string apiKey)
        {
            if (!_isRecording || _waveIn == null || _writer == null || _audioStream == null)
                return string.Empty;

            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;

            _writer.Flush();
            _writer.Dispose();
            _writer = null;

            _isRecording = false;

            byte[] audioBytes = _audioStream.ToArray();
            _audioStream.Dispose();
            _audioStream = null;

            if (audioBytes.Length < 1000) return string.Empty; // Audio too short

            return await TranscribeWithGroqWhisperAsync(audioBytes, apiKey);
        }

        private static async Task<string> TranscribeWithGroqWhisperAsync(byte[] wavBytes, string apiKey)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/audio/transcriptions");
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var content = new MultipartFormDataContent();
            using var byteArrayContent = new ByteArrayContent(wavBytes);
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

            content.Add(byteArrayContent, "file", "speech.wav");
            content.Add(new StringContent("whisper-large-v3"), "model");
            content.Add(new StringContent("en"), "language");

            request.Content = content;

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return string.Empty;

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            return doc.RootElement.TryGetProperty("text", out var textProp) 
                ? textProp.GetString() ?? string.Empty 
                : string.Empty;
        }
    }
}
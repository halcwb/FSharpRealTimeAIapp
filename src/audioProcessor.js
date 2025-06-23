// src/audioProcessor.js
export class AudioProcessor {
    constructor() {
        this.audioContext = null;
        this.workletNode = null;
        this.micStream = null;
        this.isMuted = false;
    }

    async start(onAudioData) {
        try {
            this.micStream = await navigator.mediaDevices.getUserMedia({ 
                video: false, 
                audio: { sampleRate: 16000 } 
            });

            this.audioContext = new AudioContext({ sampleRate: 24000 });
            const micStreamSource = this.audioContext.createMediaStreamSource(this.micStream);

            // Create audio worklet for processing
            const workletBlobUrl = URL.createObjectURL(new Blob([`
                registerProcessor('audio-processor', class extends AudioWorkletProcessor {
                    constructor() { super(); }
                    process(input, output, parameters) {
                        this.port.postMessage(input[0]);
                        return true;
                    }
                });
            `], { type: 'application/javascript' }));

            await this.audioContext.audioWorklet.addModule(workletBlobUrl);
            this.workletNode = new AudioWorkletNode(this.audioContext, 'audio-processor');
            
            micStreamSource.connect(this.workletNode);
            
            this.workletNode.port.onmessage = (e) => {
                if (this.isMuted) return;

                // Convert float32 to int16
                const float32Samples = e.data[0];
                if (!float32Samples) return;
                
                const numSamples = float32Samples.length;
                const int16Samples = new Int16Array(numSamples);
                for (let i = 0; i < numSamples; i++) {
                    int16Samples[i] = float32Samples[i] * 0x7FFF;
                }
                
                const uint8Array = new Uint8Array(int16Samples.buffer);
                onAudioData(Array.from(uint8Array));
            };

            return this.micStream;
        } catch (error) {
            console.error('Failed to start audio:', error);
            throw error;
        }
    }

    setMute(stream, mute) {
        this.isMuted = mute;
        if (stream) {
            stream.getAudioTracks().forEach(track => {
                track.enabled = !mute;
            });
        }
    }

    dispose(stream) {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
        }
        if (this.workletNode) {
            this.workletNode.disconnect();
        }
        if (this.audioContext) {
            this.audioContext.close();
        }
    }
}
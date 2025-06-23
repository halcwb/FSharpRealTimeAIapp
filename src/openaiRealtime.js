// src/openaiRealtime.js

// Import AudioProcessor from the same directory
import { AudioProcessor } from './AudioProcessor.js';

export class RealtimeManager {
    constructor() {
        this.session = null;
        this.isConnected = false;
        this.apiKey = import.meta.env.VITE_OPENAI_API_KEY || 'your-api-key-here';
        this.audioContext = null;
        this.audioQueue = [];
        this.isPlaying = false;
    }

    async startSession(modelDescription, onModelUpdate, onMessage) {
        try {
            onMessage('Connecting to OpenAI...');
            
            // Initialize audio context for playback
            this.audioContext = new AudioContext({ sampleRate: 24000 });
            
            // Connect to OpenAI Realtime API
            const response = await fetch('https://api.openai.com/v1/realtime/sessions', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.apiKey}`,
                    'Content-Type': 'application/json',
                    'OpenAI-Beta': 'realtime=v1'
                },
                body: JSON.stringify({
                    model: 'gpt-4o-realtime-preview',
                    voice: 'alloy',
                    instructions: `You are helping to edit a JSON object that represents a ${modelDescription}.
                        Listen to the user and collect information from them. Do not reply to them unless they explicitly
                        ask for your input; just listen.
                        Each time they provide information that can be added to the JSON object, add it to the existing object,
                        and then call the tool to save the updated object. Don't stop updating the JSON object.
                        Even if you think the information is incorrect, accept it - do not try to correct mistakes.
                        After each time you have called the JSON updating tool, just reply OK.`,
                    modalities: ['text', 'audio'],
                    tools: [{
                        name: 'Save_ModelData',
                        description: 'Save the updated car model data',
                        parameters: {
                            type: 'object',
                            properties: {
                                make: { type: 'string' },
                                model: { type: 'string' },
                                year: { type: 'integer' },
                                mileage: { type: 'integer' },
                                conditionNotes: { 
                                    type: 'array',
                                    items: { type: 'string' }
                                },
                                tyres: {
                                    type: 'object',
                                    properties: {
                                        frontLeft: { type: 'string', enum: ['NeedsReplacement', 'Worn', 'Good', 'New'] },
                                        frontRight: { type: 'string', enum: ['NeedsReplacement', 'Worn', 'Good', 'New'] },
                                        backLeft: { type: 'string', enum: ['NeedsReplacement', 'Worn', 'Good', 'New'] },
                                        backRight: { type: 'string', enum: ['NeedsReplacement', 'Worn', 'Good', 'New'] }
                                    }
                                }
                            }
                        }
                    }]
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const sessionData = await response.json();
            this.session = sessionData;
            
            // Start WebSocket connection for real-time communication
            await this.connectWebSocket(onModelUpdate, onMessage);
            
            onMessage('Connected successfully');
        } catch (error) {
            console.error('Failed to start session:', error);
            onMessage(`Connection failed: ${error.message}`);
            throw error;
        }
    }

    async connectWebSocket(onModelUpdate, onMessage) {
        const wsUrl = `wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview`;
        
        this.ws = new WebSocket(wsUrl, [], {
            headers: {
                'Authorization': `Bearer ${this.apiKey}`,
                'OpenAI-Beta': 'realtime=v1'
            }
        });

        this.ws.onopen = () => {
            this.isConnected = true;
            onMessage('WebSocket connected');
        };

        this.ws.onmessage = async (event) => {
            try {
                const data = JSON.parse(event.data);
                await this.handleRealtimeEvent(data, onModelUpdate, onMessage);
            } catch (error) {
                console.error('Error handling message:', error);
            }
        };

        this.ws.onclose = () => {
            this.isConnected = false;
            onMessage('Connection closed');
        };

        this.ws.onerror = (error) => {
            console.error('WebSocket error:', error);
            onMessage('Connection error occurred');
        };
    }

    async handleRealtimeEvent(event, onModelUpdate, onMessage) {
        switch (event.type) {
            case 'session.created':
                onMessage('Session created');
                break;

            case 'input_audio_buffer.speech_started':
                onMessage('Speech started');
                this.clearAudioQueue();
                break;

            case 'input_audio_buffer.speech_stopped':
                onMessage('Speech stopped');
                break;

            case 'response.audio.delta':
                if (event.delta) {
                    await this.enqueueAudio(event.delta);
                }
                break;

            case 'response.text.delta':
                if (event.delta) {
                    onMessage(event.delta);
                }
                break;

            case 'response.function_call_arguments.done':
                if (event.name === 'Save_ModelData') {
                    try {
                        const carData = JSON.parse(event.arguments);
                        const convertedData = this.convertToFSharpModel(carData);
                        onModelUpdate(convertedData);
                    } catch (error) {
                        console.error('Error parsing function arguments:', error);
                    }
                }
                break;

            case 'response.done':
                onMessage('Response completed');
                break;

            default:
                console.log('Unhandled event type:', event.type);
        }
    }

    convertToFSharpModel(jsModel) {
        const tyreStatusMap = {
            'NeedsReplacement': 'NeedsReplacement',
            'Worn': 'Worn',
            'Good': 'Good',
            'New': 'New'
        };

        return {
            Make: jsModel.make || null,
            Model: jsModel.model || null,
            Year: jsModel.year || null,
            Mileage: jsModel.mileage || null,
            ConditionNotes: jsModel.conditionNotes || [],
            Tyres: {
                FrontLeft: jsModel.tyres?.frontLeft ? tyreStatusMap[jsModel.tyres.frontLeft] : null,
                FrontRight: jsModel.tyres?.frontRight ? tyreStatusMap[jsModel.tyres.frontRight] : null,
                BackLeft: jsModel.tyres?.backLeft ? tyreStatusMap[jsModel.tyres.backLeft] : null,
                BackRight: jsModel.tyres?.backRight ? tyreStatusMap[jsModel.tyres.backRight] : null
            }
        };
    }

    async sendAudio(audioData) {
        if (!this.isConnected || !this.ws) {
            throw new Error('Not connected to OpenAI');
        }

        // Convert byte array to base64
        const base64Audio = btoa(String.fromCharCode(...audioData));
        
        const message = {
            type: 'input_audio_buffer.append',
            audio: base64Audio
        };

        this.ws.send(JSON.stringify(message));
    }

    async updateModel(carModel) {
        if (!this.isConnected || !this.ws) {
            return;
        }

        const jsModel = this.convertFromFSharpModel(carModel);
        const message = {
            type: 'conversation.item.create',
            item: {
                type: 'message',
                role: 'user',
                content: [{
                    type: 'text',
                    text: `The current modelData value is ${JSON.stringify(jsModel)}. When updating this later, include all these same values if they are unchanged (or they will be overwritten with nulls).`
                }]
            }
        };

        this.ws.send(JSON.stringify(message));
    }

    convertFromFSharpModel(fsharpModel) {
        const tyreStatusMap = {
            'NeedsReplacement': 'NeedsReplacement',
            'Worn': 'Worn',
            'Good': 'Good',
            'New': 'New'
        };

        return {
            make: fsharpModel.Make,
            model: fsharpModel.Model,
            year: fsharpModel.Year,
            mileage: fsharpModel.Mileage,
            conditionNotes: fsharpModel.ConditionNotes,
            tyres: {
                frontLeft: fsharpModel.Tyres.FrontLeft ? tyreStatusMap[fsharpModel.Tyres.FrontLeft] : null,
                frontRight: fsharpModel.Tyres.FrontRight ? tyreStatusMap[fsharpModel.Tyres.FrontRight] : null,
                backLeft: fsharpModel.Tyres.BackLeft ? tyreStatusMap[fsharpModel.Tyres.BackLeft] : null,
                backRight: fsharpModel.Tyres.BackRight ? tyreStatusMap[fsharpModel.Tyres.BackRight] : null
            }
        };
    }

    async enqueueAudio(audioData) {
        try {
            // Decode base64 audio data
            const binaryString = atob(audioData);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }

            // Convert to int16 then float32 for Web Audio API
            const int16Array = new Int16Array(bytes.buffer);
            const float32Array = new Float32Array(int16Array.length);
            for (let i = 0; i < int16Array.length; i++) {
                float32Array[i] = int16Array[i] / 0x7FFF;
            }

            // Create audio buffer
            const audioBuffer = this.audioContext.createBuffer(1, float32Array.length, 24000);
            audioBuffer.copyToChannel(float32Array, 0);

            this.audioQueue.push(audioBuffer);
            
            if (!this.isPlaying) {
                this.playNextAudio();
            }
        } catch (error) {
            console.error('Error processing audio:', error);
        }
    }

    playNextAudio() {
        if (this.audioQueue.length === 0) {
            this.isPlaying = false;
            return;
        }

        this.isPlaying = true;
        const audioBuffer = this.audioQueue.shift();
        const source = this.audioContext.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(this.audioContext.destination);
        
        source.onended = () => {
            this.playNextAudio();
        };

        source.start();
    }

    clearAudioQueue() {
        this.audioQueue = [];
        this.isPlaying = false;
    }

    dispose() {
        if (this.ws) {
            this.ws.close();
        }
        if (this.audioContext) {
            this.audioContext.close();
        }
        this.clearAudioQueue();
    }
}

// Export singleton instances
export const audioProcessor = new AudioProcessor();
export const realtimeClient = new RealtimeManager();

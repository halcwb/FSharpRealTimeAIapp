# F# Real-time Voice Form with OpenAI Integration

This project is an F# implementation using Fable and Material-UI that provides the same functionality as the original Blazor/C# real-time voice form application. It allows users to fill out a car listing form using voice input powered by OpenAI's real-time API.

## Features

- **Real-time Voice Input**: Speak to fill out form fields automatically
- **Material-UI Interface**: Modern, responsive design using Material-UI components
- **F# with Fable**: Type-safe functional programming compiled to JavaScript
- **OpenAI Integration**: Uses OpenAI's real-time API for voice processing and form filling
- **Form Validation**: Client-side validation with error handling
- **Audio Processing**: Real-time audio capture and streaming

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node.js](https://nodejs.org/) (v16 or higher)
- [npm](https://www.npmjs.com/) or [yarn](https://yarnpkg.com/)
- OpenAI API key with access to the real-time API

## Project Structure

```
src/
├── Types.fs              # Type definitions for the car model and UI state
├── AudioUtils.fs         # Audio processing utilities and interop
├── OpenAIClient.fs       # OpenAI real-time API client integration
├── Components.fs         # Reusable UI components
├── App.fs               # Main application logic and view
├── audioProcessor.js    # JavaScript audio processing module
└── openaiRealtime.js    # JavaScript OpenAI integration module

public/
└── index.html           # HTML template

package.json             # Node.js dependencies
webpack.config.js        # Webpack configuration
.fablerc                # Fable compiler configuration
RealtimeVoiceForm.fsproj # F# project file
```

## Setup Instructions

### 1. Clone and Install Dependencies

```bash
# Install .NET dependencies
dotnet restore

# Install Node.js dependencies
npm install
```

### 2. Set Up Environment Variables

Create a `.env` file in the root directory:

```env
OPENAI_API_KEY=your_openai_api_key_here
```

### 3. Build and Run

#### Development Mode

```bash
# Start the development server
npm start
```

This will:
- Compile F# code to JavaScript using Fable
- Start webpack dev server on http://localhost:8080
- Enable hot reloading for development

#### Production Build

```bash
# Build for production
npm run build:prod
```

## Usage

1. **Start the Application**: Open http://localhost:8080 in your browser
2. **Enable Microphone**: Click the microphone button to start voice input
3. **Speak Naturally**: Describe the car you want to list, e.g.:
   - "This is a 2018 Honda Civic with 45,000 miles"
   - "The front tires are in good condition, but the rear ones need replacement"
   - "It has leather seats and a sunroof"
4. **Review and Save**: The AI will populate the form fields automatically
5. **Manual Editing**: You can also edit fields manually at any time
6. **Validation**: Click "Save" to validate the form

## Voice Commands Examples

The AI assistant understands natural language and will extract relevant information:

- **Basic Info**: "2020 Toyota Camry with 32,000 miles"
- **Condition**: "Excellent condition, well maintained, no accidents"
- **Features**: "Has backup camera, heated seats, and navigation system"
- **Tires**: "Front tires are new, rear tires are worn and need replacing soon"

## Architecture

### F# Components

- **Types.fs**: Defines the domain model using F# discriminated unions and records
- **App.fs**: Implements the Elmish architecture (Model-View-Update pattern)
- **Components.fs**: Reusable UI components built with Feliz and Material-UI

### JavaScript Interop

- **audioProcessor.js**: Handles microphone access and audio stream processing
- **openaiRealtime.js**: Manages WebSocket connection to OpenAI's real-time API

### Key Technologies

- **Fable**: F# to JavaScript compiler
- **Feliz**: Type-safe React bindings for F#
- **Material-UI**: React component library for modern UI
- **Elmish**: Functional reactive programming model
- **WebAudio API**: For real-time audio processing
- **OpenAI Real-time API**: For voice-to-form conversion

## Customization

### Adding New Form Fields

1. Update the `CarDescriptor` type in `Types.fs`
2. Add corresponding UI components in `Components.fs`
3. Update the validation logic in `App.fs`
4. Modify the OpenAI tool schema in `openaiRealtime.js`

### Styling

The application uses Material-UI theming. You can customize:
- Colors and typography in the theme provider
- Component-specific styling using the `sx` prop
- Global styles in the HTML template

### Voice Processing

Customize the AI instructions in `openaiRealtime.js` to:
- Change the assistant's personality
- Modify how it interprets voice input
- Add domain-specific knowledge

## Troubleshooting

### Common Issues

1. **Microphone Access Denied**: Ensure HTTPS is used in production
2. **OpenAI API Errors**: Verify your API key and rate limits
3. **Build Errors**: Check that all .NET and Node.js dependencies are installed
4. **Audio Issues**: Test in different browsers; Chrome works best

### Development Tips

- Use browser developer tools to inspect the generated JavaScript
- Check the console for F# compilation errors
- Monitor network requests to debug OpenAI API calls
- Use the activity log to trace voice processing events

## Deployment

### Production Deployment

1. Set environment variables in your hosting platform
2. Build the production bundle: `npm run build:prod`
3. Serve the `dist/` folder using a static file server
4. Ensure HTTPS is configured for microphone access

### Environment Considerations

- **HTTPS Required**: Microphone access requires secure context
- **CORS**: Configure CORS if API calls are made to different domains
- **Rate Limits**: Monitor OpenAI API usage and implement appropriate limits

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

[Specify your license here]

## Support

For issues and questions:
- Check the browser console for error messages
- Review the OpenAI API documentation
- Test microphone permissions in browser settings
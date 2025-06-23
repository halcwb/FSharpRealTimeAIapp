module App

open System
open Elmish
open Browser
open Fable.Core
open Fable.React
open Fable.Core.JsInterop
open Types
open Components
open AudioUtils
open OpenAIClient
open Browser.Types

type State = {
    Car: CarDescriptor
    MicStatus: MicStatus
    MicStream: obj option
    Messages: string list
    IsValidated: bool
    IsConnecting: bool
    Error: string option
}

let init () = 
    {
        Car = {
            Make = None
            Model = None
            Year = None
            Mileage = None
            ConditionNotes = []
            Tyres = {
                FrontLeft = None
                FrontRight = None
                BackLeft = None
                BackRight = None
            }
        }
        MicStatus = Disconnected
        MicStream = None
        Messages = []
        IsValidated = false
        IsConnecting = false
        Error = None
    }, Cmd.none

let validateCar (car: CarDescriptor) =
    let errors = ResizeArray<string>()
    
    if car.Make.IsNone then errors.Add("Make is required")
    if car.Model.IsNone then errors.Add("Model is required")
    if car.Year.IsNone then errors.Add("Year is required")
    if car.Mileage.IsNone then errors.Add("Mileage is required")
    
    match car.Year with
    | Some year when year < 1900 || year > 2100 -> errors.Add("Year must be between 1900 and 2100")
    | _ -> ()
    
    match car.Mileage with
    | Some mileage when mileage < 0 || mileage > 2000000 -> errors.Add("Mileage must be between 0 and 2,000,000")
    | _ -> ()
    
    if car.Tyres.FrontLeft.IsNone then errors.Add("Front left tyre status is required")
    if car.Tyres.FrontRight.IsNone then errors.Add("Front right tyre status is required")
    if car.Tyres.BackLeft.IsNone then errors.Add("Back left tyre status is required")
    if car.Tyres.BackRight.IsNone then errors.Add("Back right tyre status is required")
    
    errors |> List.ofSeq

let update (msg: Msg) (state: State) =
    match msg with
    | UpdateMake value ->
        { state with Car = { state.Car with Make = if System.String.IsNullOrEmpty(value) then None else Some value } }, Cmd.none
    
    | UpdateModel value ->
        { state with Car = { state.Car with Model = if System.String.IsNullOrEmpty(value) then None else Some value } }, Cmd.none
    
    | UpdateYear value ->
        { state with Car = { state.Car with Year = value } }, Cmd.none
    
    | UpdateMileage value ->
        { state with Car = { state.Car with Mileage = value } }, Cmd.none
    
    | AddConditionNote ->
        { state with Car = { state.Car with ConditionNotes = state.Car.ConditionNotes @ [""] } }, Cmd.none
    
    | UpdateConditionNote (index, value) ->
        let newNotes = 
            state.Car.ConditionNotes 
            |> List.mapi (fun i note -> if i = index then value else note)
        { state with Car = { state.Car with ConditionNotes = newNotes } }, Cmd.none
    
    | RemoveConditionNote index ->
        let newNotes = 
            state.Car.ConditionNotes 
            |> List.mapi (fun i note -> i, note)
            |> List.filter (fun (i, _) -> i <> index)
            |> List.map snd
        { state with Car = { state.Car with ConditionNotes = newNotes } }, Cmd.none
    
    | UpdateTyre (position, status) ->
        let newTyres = 
            match position with
            | "FrontLeft" -> { state.Car.Tyres with FrontLeft = status }
            | "FrontRight" -> { state.Car.Tyres with FrontRight = status }
            | "BackLeft" -> { state.Car.Tyres with BackLeft = status }
            | "BackRight" -> { state.Car.Tyres with BackRight = status }
            | _ -> state.Car.Tyres
        { state with Car = { state.Car with Tyres = newTyres } }, Cmd.none
    
    | ToggleMic ->
        match state.MicStatus with
        | Disconnected ->
            console.log processor
            let startAudio () = 
                processor.start (fun audioData -> ())
            let cmd = Cmd.OfPromise.either startAudio () (fun stream -> MicConnected) (fun ex -> MessageAdded $"Microphone error: {ex.Message}")
            { state with MicStatus = Active; IsConnecting = true }, cmd
        | Active ->
            state.MicStream |> Option.iter (fun stream -> processor.setMute stream true)
            { state with MicStatus = Muted }, Cmd.none
        | Muted ->
            state.MicStream |> Option.iter (fun stream -> processor.setMute stream false)
            { state with MicStatus = Active }, Cmd.none
    
    | MicConnected ->
        let startSession () = 
            manager.startSession "Car to be listed for sale" (fun model -> ()) (fun message -> ())
        let cmd = Cmd.OfPromise.either startSession () (fun _ -> MessageAdded "Connected to AI assistant") (fun ex -> MessageAdded $"Connection error: {ex.Message}")
        { state with IsConnecting = false }, cmd
    
    | AudioDataReceived audioData ->
        let cmd = Cmd.OfPromise.attempt (manager.sendAudio) audioData (fun _ -> MessageAdded "Audio sent")
        state, cmd
    
    | ModelUpdated newCar ->
        let cmd = Cmd.OfPromise.attempt (manager.updateModel) newCar (fun _ -> MessageAdded "Model updated")
        { state with Car = newCar }, cmd
    
    | MessageAdded message ->
        { state with Messages = state.Messages @ [message]; Error = None }, Cmd.none
    
    | ValidationRequested ->
        let validationErrors = validateCar state.Car
        if List.isEmpty validationErrors then
            { state with IsValidated = true }, Cmd.ofMsg (MessageAdded "Car data is valid!")
        else
            { state with Error = Some (String.concat "; " validationErrors) }, Cmd.none


let inline toReact (el: JSX.Element) : ReactElement = unbox el


[<JSX.Component>]
let View (state: State) (dispatch: Msg -> unit) =
    let handleMakeChange = fun ev -> ev?target?value |> UpdateMake |> dispatch
    let handleModelChange = fun ev -> ev?target?value |> UpdateModel |> dispatch
    let handleYearChange = fun ev -> 
        let value: string = ev?target?value
        match System.Int32.TryParse(value) with
        | true, year -> dispatch (UpdateYear (Some year))
        | false, _ -> dispatch (UpdateYear None)
    let handleMileageChange = fun ev ->
        let value: string = ev?target?value
        match System.Int32.TryParse(value) with
        | true, mileage -> dispatch (UpdateMileage (Some mileage))
        | false, _ -> dispatch (UpdateMileage None)
    
    let handleValidationClick = fun _ -> dispatch ValidationRequested
    let handleAddNoteClick = fun _ -> dispatch AddConditionNote

    let conditionNotesElements =
        state.Car.ConditionNotes
        |> List.mapi (fun i note ->
            let handleNoteChange = fun ev -> ev?target?value |> fun value -> dispatch (UpdateConditionNote (i, value))
            let handleRemoveClick = fun _ -> dispatch (RemoveConditionNote i)
            
            JSX.jsx
                $"""
            import Grid from '@mui/material/Grid';
            import TextField from '@mui/material/TextField';
            import IconButton from '@mui/material/IconButton';
            import CloseIcon from '@mui/icons-material/Close';

            <Grid container spacing={1} alignItems="center" sx={ {| marginTop = 1 |} } key={i}>
                <Grid item xs>
                    <TextField
                        fullWidth
                        multiline
                        value={note}
                        onChange={handleNoteChange}
                    />
                </Grid>
                <Grid item>
                    <IconButton onClick={handleRemoveClick}>
                        <CloseIcon />
                    </IconButton>
                </Grid>
            </Grid>
            """)

    let messagesLog =
        if not (List.isEmpty state.Messages) then
            let messageElements =
                state.Messages
                |> List.map (fun message ->
                    JSX.jsx $"""<li key={message}>{message}</li>""")

            JSX.jsx
                $"""
            import Paper from '@mui/material/Paper';
            import Typography from '@mui/material/Typography';

            <Paper sx={ {| padding = 2; marginTop = 2 |} }>
                <Typography variant="h6">
                    Log ({List.length state.Messages})
                </Typography>
                <ul>
                    {messageElements}
                </ul>
            </Paper>
            """
        else
            JSX.jsx "<></>"

    let snackbarNotification =
        match List.tryLast state.Messages with
        | Some lastMessage ->
            JSX.jsx
                $"""
            import Snackbar from '@mui/material/Snackbar';

            <Snackbar
                open={true}
                message={lastMessage}
                anchorOrigin={ {| vertical = "bottom"; horizontal = "center" |} }
            />
            """
        | None -> JSX.jsx "<></>"

    JSX.jsx
        $"""
    import Container from '@mui/material/Container';
    import AppBar from '@mui/material/AppBar';
    import Toolbar from '@mui/material/Toolbar';
    import Typography from '@mui/material/Typography';
    import Button from '@mui/material/Button';
    import Paper from '@mui/material/Paper';
    import Grid from '@mui/material/Grid';
    import TextField from '@mui/material/TextField';

    <Container maxWidth="lg">
        <AppBar position="static">
            <Toolbar>
                <Typography variant="h6" component="h1" sx={ {| flexGrow = 1 |} }>
                    Add a new vehicle
                </Typography>
                {MicButton.View {| status = state.MicStatus; onClick = fun () -> dispatch ToggleMic |}}
                <Button
                    variant="contained"
                    color="primary"
                    onClick={handleValidationClick}
                    startIcon={Mui.Icons.SaveIcon}
                >
                    Save
                </Button>
            </Toolbar>
        </AppBar>

        <Paper elevation={3} sx={ {| padding = 3; marginTop = 2 |} }>
            <Grid container spacing={3}>
                <Grid item xs={12}>
                    <Paper sx={ {| padding = 2; backgroundColor = "#f5f5f5" |} }>
                        <Grid container spacing={2}>
                            <Grid item xs={12} sm={6} md={3}>
                                <TextField
                                    label="Make"
                                    fullWidth
                                    value={state.Car.Make |> Option.defaultValue ""}
                                    onChange={handleMakeChange}
                                />
                            </Grid>
                            <Grid item xs={12} sm={6} md={3}>
                                <TextField
                                    label="Model"
                                    fullWidth
                                    value={state.Car.Model |> Option.defaultValue ""}
                                    onChange={handleModelChange}
                                />
                            </Grid>
                            <Grid item xs={12} sm={6} md={3}>
                                <TextField
                                    label="Year"
                                    fullWidth
                                    type="number"
                                    value={state.Car.Year |> Option.map string |> Option.defaultValue ""}
                                    onChange={handleYearChange}
                                />
                            </Grid>
                            <Grid item xs={12} sm={6} md={3}>
                                <TextField
                                    label="Mileage"
                                    fullWidth
                                    type="number"
                                    value={state.Car.Mileage |> Option.map string |> Option.defaultValue ""}
                                    onChange={handleMileageChange}
                                />
                            </Grid>
                        </Grid>
                    </Paper>
                </Grid>

                <Grid item xs={12} lg={7}>
                    <Paper sx={ {| padding = 2; backgroundColor = "#f5f5f5" |} }>
                        <Typography variant="h6" gutterBottom>
                            Condition / Features
                        </Typography>
                        <div>
                            {conditionNotesElements}
                        </div>
                        <Button
                            variant="contained"
                            onClick={handleAddNoteClick}
                            startIcon={Mui.Icons.AddIcon}
                            sx={ {| marginTop = 2 |} }
                        >
                            Add entry
                        </Button>
                    </Paper>
                </Grid>

                <Grid item xs={12} lg={5}>
                    <Paper sx={ {| padding = 2; backgroundColor = "#f5f5f5" |} }>
                        <Typography variant="h6" gutterBottom>
                            Tyres
                        </Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6}>
                                <Typography variant="subtitle2">
                                    Front Left
                                </Typography>
                                {Components.TyreStatusPicker.View {| value = state.Car.Tyres.FrontLeft; onChange = fun status -> dispatch (UpdateTyre ("FrontLeft", status)) |}}
                            </Grid>
                            <Grid item xs={6}>
                                <Typography variant="subtitle2">
                                    Front Right
                                </Typography>
                                {Components.TyreStatusPicker.View {| value = state.Car.Tyres.FrontRight; onChange = fun status -> dispatch (UpdateTyre ("FrontRight", status)) |}}
                            </Grid>
                            <Grid item xs={6}>
                                <Typography variant="subtitle2">
                                    Back Left
                                </Typography>
                                {Components.TyreStatusPicker.View {| value = state.Car.Tyres.BackLeft; onChange = fun status -> dispatch (UpdateTyre ("BackLeft", status)) |}}
                            </Grid>
                            <Grid item xs={6}>
                                <Typography variant="subtitle2">
                                    Back Right
                                </Typography>
                                {Components.TyreStatusPicker.View {| value = state.Car.Tyres.BackRight; onChange = fun status -> dispatch (UpdateTyre ("BackRight", status)) |}}
                            </Grid>
                        </Grid>
                    </Paper>
                </Grid>
            </Grid>
        </Paper>

        {messagesLog}
        {snackbarNotification}
    </Container>
    """
    |> toReact

open Elmish.React

Program.mkProgram init update View
|> Program.withReactSynchronous "root"
|> Program.run
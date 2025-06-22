module App

open System
open Elmish
open Feliz
open Feliz.MaterialUI
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

let view (state: State) (dispatch: Msg -> unit) =
    Mui.container [
        container.maxWidth.lg
        container.children [
            Mui.appBar [
                appBar.position.``static``
                appBar.children [
                    Mui.toolbar [
                        Mui.typography [
                            typography.variant.h6
                            typography.component' "h1"
                            typography.sx [ style.flexGrow 1 ]
                            typography.children "Add a new vehicle"
                        ]
                        micButton state.MicStatus (fun () -> dispatch ToggleMic)
                        Mui.button [
                            button.variant.contained
                            button.color.primary
                            button.onClick (fun _ -> dispatch ValidationRequested)
                            button.startIcon (Mui.icon "save")
                            button.children "Save"
                        ]
                    ]
                ]
            ]
            
            Mui.paper [
                paper.elevation 3
                paper.sx [ style.padding 3; style.marginTop 2 ]
                paper.children [
                    Mui.grid [
                        grid.container true
                        grid.spacing 3
                        grid.children [
                            // Basic info section
                            Mui.grid [
                                grid.item true
                                grid.xs 12
                                grid.children [
                                    Mui.paper [
                                        paper.sx [ style.padding 2; style.backgroundColor "#f5f5f5" ]
                                        paper.children [
                                            Mui.grid [
                                                grid.container true
                                                grid.spacing 2
                                                grid.children [
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 12
                                                        grid.sm 6
                                                        grid.md 3
                                                        grid.children [
                                                            Mui.textField [
                                                                textField.label "Make"
                                                                textField.fullWidth true
                                                                textField.value (state.Car.Make |> Option.defaultValue "")
                                                                textField.onChange (UpdateMake >> dispatch)
                                                            ]
                                                        ]
                                                    ]
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 12
                                                        grid.sm 6
                                                        grid.md 3
                                                        grid.children [
                                                            Mui.textField [
                                                                textField.label "Model"
                                                                textField.fullWidth true
                                                                textField.value (state.Car.Model |> Option.defaultValue "")
                                                                textField.onChange (UpdateModel >> dispatch)
                                                            ]
                                                        ]
                                                    ]
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 12
                                                        grid.sm 6
                                                        grid.md 3
                                                        grid.children [
                                                            Mui.textField [
                                                                textField.label "Year"
                                                                textField.fullWidth true
                                                                textField.type' "number"
                                                                textField.value (state.Car.Year |> Option.map string |> Option.defaultValue "")
                                                                textField.onChange (fun value -> 
                                                                    match System.Int32.TryParse(value) with
                                                                    | true, year -> dispatch (UpdateYear (Some year))
                                                                    | false, _ -> dispatch (UpdateYear None)
                                                                )
                                                            ]
                                                        ]
                                                    ]
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 12
                                                        grid.sm 6
                                                        grid.md 3
                                                        grid.children [
                                                            Mui.textField [
                                                                textField.label "Mileage"
                                                                textField.fullWidth true
                                                                textField.type' "number"
                                                                textField.value (state.Car.Mileage |> Option.map string |> Option.defaultValue "")
                                                                textField.onChange (fun value -> 
                                                                    match System.Int32.TryParse(value) with
                                                                    | true, mileage -> dispatch (UpdateMileage (Some mileage))
                                                                    | false, _ -> dispatch (UpdateMileage None)
                                                                )
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            
                            // Condition notes section
                            Mui.grid [
                                grid.item true
                                grid.xs 12
                                grid.lg 7
                                grid.children [
                                    Mui.paper [
                                        paper.sx [ style.padding 2; style.backgroundColor "#f5f5f5" ]
                                        paper.children [
                                            Mui.typography [
                                                typography.variant.h6
                                                typography.gutterBottom true
                                                typography.children "Condition / Features"
                                            ]
                                            Html.div [
                                                for i, note in List.indexed state.Car.ConditionNotes do
                                                    Mui.grid [
                                                        grid.container true
                                                        grid.spacing 1
                                                        grid.alignItems.center
                                                        grid.sx [ style.marginTop 1 ]
                                                        grid.children [
                                                            Mui.grid [
                                                                grid.item true
                                                                grid.xs true
                                                                grid.children [
                                                                    Mui.textField [
                                                                        textField.fullWidth true
                                                                        textField.multiline true
                                                                        textField.value note
                                                                        textField.onChange (fun value -> dispatch (UpdateConditionNote (i, value)))
                                                                    ]
                                                                ]
                                                            ]
                                                            Mui.grid [
                                                                grid.item true
                                                                grid.children [
                                                                    Mui.iconButton [
                                                                        iconButton.onClick (fun _ -> dispatch (RemoveConditionNote i))
                                                                        iconButton.children [
                                                                            Mui.icon "close"
                                                                        ]
                                                                    ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                            ]
                                            Mui.button [
                                                button.variant.contained
                                                button.onClick (fun _ -> dispatch AddConditionNote)
                                                button.startIcon (Mui.icon "add")
                                                button.sx [ style.marginTop 2 ]
                                                button.children "Add entry"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            
                            // Tyres section
                            Mui.grid [
                                grid.item true
                                grid.xs 12
                                grid.lg 5
                                grid.children [
                                    Mui.paper [
                                        paper.sx [ style.padding 2; style.backgroundColor "#f5f5f5" ]
                                        paper.children [
                                            Mui.typography [
                                                typography.variant.h6
                                                typography.gutterBottom true
                                                typography.children "Tyres"
                                            ]
                                            Mui.grid [
                                                grid.container true
                                                grid.spacing 2
                                                grid.children [
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 6
                                                        grid.children [
                                                            Mui.typography [
                                                                typography.variant.subtitle2
                                                                typography.children "Front Left"
                                                            ]
                                                            tyreStatusPicker state.Car.Tyres.FrontLeft (fun status -> 
                                                                dispatch (UpdateTyre ("FrontLeft", status)))
                                                        ]
                                                    ]
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 6
                                                        grid.children [
                                                            Mui.typography [
                                                                typography.variant.subtitle2
                                                                typography.children "Front Right"
                                                            ]
                                                            tyreStatusPicker state.Car.Tyres.FrontRight (fun status -> 
                                                                dispatch (UpdateTyre ("FrontRight", status)))
                                                        ]
                                                    ]
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 6
                                                        grid.children [
                                                            Mui.typography [
                                                                typography.variant.subtitle2
                                                                typography.children "Back Left"
                                                            ]
                                                            tyreStatusPicker state.Car.Tyres.BackLeft (fun status -> 
                                                                dispatch (UpdateTyre ("BackLeft", status)))
                                                        ]
                                                    ]
                                                    Mui.grid [
                                                        grid.item true
                                                        grid.xs 6
                                                        grid.children [
                                                            Mui.typography [
                                                                typography.variant.subtitle2
                                                                typography.children "Back Right"
                                                            ]
                                                            tyreStatusPicker state.Car.Tyres.BackRight (fun status -> 
                                                                dispatch (UpdateTyre ("BackRight", status)))
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
            
            // Messages log
            if not (List.isEmpty state.Messages) then
                Mui.paper [
                    paper.sx [ style.padding 2; style.marginTop 2 ]
                    paper.children [
                        Mui.typography [
                            typography.variant.h6
                            typography.children $"Log ({List.length state.Messages})"
                        ]
                        Html.ul [
                            for message in state.Messages do
                                Html.li [ Html.text message ]
                        ]
                    ]
                ]
            
            // Latest message notification
            match List.tryLast state.Messages with
            | Some lastMessage ->
                Mui.snackbar [
                    snackbar.open' true
                    snackbar.message lastMessage
                    snackbar.anchorOrigin.vertical.bottom
                    snackbar.anchorOrigin.horizontal.center
                ]
            | None -> Html.none
        ]
    ]

// Program entry point
open Elmish.React

Program.mkProgram init update view
|> Program.withReactSynchronous "root"
|> Program.run
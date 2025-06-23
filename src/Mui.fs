module Mui

open Fable.Core
open Fable.Core.JS

module Icons =

    [<JSX.Component>]
    let HomeIcon = JSX.jsx $"""
        import Home from '@mui/icons-material/Home';
        <Home />
    """

    [<JSX.Component>]
    let MicIcon = JSX.jsx $"""
        import Mic from '@mui/icons-material/Mic';
        <Mic />
    """

    [<JSX.Component>]
    let MicOffIcon = JSX.jsx $"""
        import MicOff from '@mui/icons-material/MicOff';
        <MicOff />
    """

    [<JSX.Component>]
    let SaveIcon = JSX.jsx $"""
        import Save from '@mui/icons-material/Save';
        <Save />
    """

    [<JSX.Component>]
    let AddIcon = JSX.jsx $"""
        import Add from '@mui/icons-material/Add';
        <Add />
    """
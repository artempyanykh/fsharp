﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if INTERACTIVE
//#r @"../../release/net40/bin/FSharp.Compiler.dll"
#r @"../../packages/NUnit.3.5.0/lib/net45/nunit.framework.dll"
#load "../../src/scripts/scriptlib.fsx" 
#load "test-framework.fs" 
#load "single-test.fs"
#else
module FSharp.Test.FSharpSuite.TypeProviderTests
#endif

open System
open System.IO
open System.Reflection
open NUnit.Framework
open TestFramework
open Scripting
open SingleTest

#if FSHARP_SUITE_DRIVES_CORECLR_TESTS
// Use these lines if you want to test CoreCLR
let FSC_BASIC = FSC_CORECLR
let FSI_BASIC = FSI_CORECLR
#else
let FSC_BASIC = FSC_OPT_PLUS_DEBUG
let FSI_BASIC = FSI_FILE
#endif

[<Test>]
let diamondAssembly () = 
    let cfg = testConfig "typeProviders/diamondAssembly"

    rm cfg "provider.dll"

    fsc cfg "%s" "--out:provided.dll -a" [".." ++ "helloWorld" ++ "provided.fs"]

    fsc cfg "%s" "--out:provider.dll -a" [".." ++ "helloWorld" ++ "provider.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test1.dll -a" cfg.fsc_flags ["test1.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2a.dll -a -r:test1.dll" cfg.fsc_flags ["test2a.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2b.dll -a -r:test1.dll" cfg.fsc_flags ["test2b.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test3.exe -r:test1.dll -r:test2a.dll -r:test2b.dll" cfg.fsc_flags ["test3.fsx"]

    peverify cfg "test1.dll"

    peverify cfg "test2a.dll"

    peverify cfg "test2b.dll"

    peverify cfg "test3.exe"

    exec cfg ("." ++ "test3.exe") ""

    use testOkFile = fileguard cfg "test.ok"

    fsi cfg "%s" cfg.fsi_flags ["test3.fsx"]

    testOkFile.CheckExists()
                
[<Test>]
let globalNamespace () = 
    let cfg = testConfig "typeProviders/globalNamespace"

    csc cfg """/out:globalNamespaceTP.dll /debug+ /target:library /r:"%s" """ cfg.FSCOREDLLPATH ["globalNamespaceTP.cs"]

    fsc cfg "%s /debug+ /r:globalNamespaceTP.dll /optimize-" cfg.fsc_flags ["test.fsx"]
                
let helloWorld p = 
    let cfg = testConfig "typeProviders/helloWorld"

    fsc cfg "%s" "--out:provided1.dll -g -a" [".." ++ "helloWorld" ++ "provided.fs"]

    fsc cfg "%s" "--out:provided2.dll -g -a" [".." ++ "helloWorld" ++ "provided.fs"]

    fsc cfg "%s" "--out:provided3.dll -g -a" [".." ++ "helloWorld" ++ "provided.fs"]

    fsc cfg "%s" "--out:provided4.dll -g -a" [".." ++ "helloWorld" ++ "provided.fs"]

    fsc cfg "%s" "--out:providedJ.dll -g -a" [".." ++ "helloWorld" ++ "providedJ.fs"]

    fsc cfg "%s" "--out:providedK.dll -g -a" [".." ++ "helloWorld" ++ "providedK.fs"]

    fsc cfg "%s" "--out:providedNullAssemblyName.dll -g -a" [".." ++ "helloWorld" ++ "providedNullAssemblyName.fsx"]

    fsc cfg "--out:provided.dll -a" [".." ++ "helloWorld" ++ "provided.fs"]

    fsc cfg "--out:providedJ.dll -a" [".." ++ "helloWorld" ++ "providedJ.fs"]

    fsc cfg "--out:providedK.dll -a" [".." ++ "helloWorld" ++ "providedK.fs"]

    fsc cfg "--out:provider.dll -a" ["provider.fsx"]

    SingleTest.singleTestBuildAndRunAux cfg p 


    rm cfg "provider_with_binary_compat_changes.dll"

    mkdir cfg "bincompat1"

    log "pushd bincompat1"
    let bincompat1 = getfullpath cfg "bincompat1"

    Directory.EnumerateFiles(bincompat1 ++ "..", "*.dll")
    |> Seq.iter (fun from -> Commands.copy_y bincompat1 from ("." ++ Path.GetFileName(from)) |> ignore)

    fscIn cfg bincompat1 "%s" "-g -a -o:test_lib.dll -r:provider.dll" [".." ++ "test.fsx"]

    fscIn cfg bincompat1 "%s" "-r:test_lib.dll -r:provider.dll" [".." ++ "testlib_client.fsx"]

    log "popd"

    mkdir cfg "bincompat2"
        
    log "pushd bincompat2"
    let bincompat2 = getfullpath cfg "bincompat2"

    Directory.EnumerateFiles(bincompat2 ++ ".." ++ "bincompat1", "*.dll")
    |> Seq.iter (fun from -> Commands.copy_y bincompat2 from ("." ++ Path.GetFileName(from)) |> ignore)

    fscIn cfg bincompat2 "%s" "--define:ADD_AN_OPTIONAL_STATIC_PARAMETER --define:USE_IMPLICIT_ITypeProvider2 --out:provider.dll -g -a" [".." ++ "provider.fsx"]

    fscIn cfg bincompat2 "-g -a -o:test_lib_recompiled.dll -r:provider.dll" [".." ++ "test.fsx"]

    fscIn cfg bincompat2 "%s" "--define:ADD_AN_OPTIONAL_STATIC_PARAMETER -r:test_lib.dll -r:provider.dll" [".." ++ "testlib_client.fsx"]

    peverify cfg (bincompat2 ++ "provider.dll")

    peverify cfg (bincompat2 ++ "test_lib.dll")

    peverify cfg (bincompat2 ++ "test_lib_recompiled.dll")

    peverify cfg (bincompat2 ++ "testlib_client.exe")

[<Test>]
let ``helloWorld fsc`` () = helloWorld FSC_BASIC

[<Test>]
let ``helloWorld fsi`` () = helloWorld FSI_STDIN


[<Test>]
let helloWorldCSharp () = 
    let cfg = testConfig "typeProviders/helloWorldCSharp"

    rm cfg "magic.dll"

    fsc cfg "%s" "--out:magic.dll -a --keyfile:magic.snk" ["magic.fs "]

    rm cfg "provider.dll"

    csc cfg """/out:provider.dll /target:library "/r:%s" /r:magic.dll""" cfg.FSCOREDLLPATH ["provider.cs"]

    fsc cfg "%s /debug+ /r:provider.dll /optimize-" cfg.fsc_flags ["test.fsx"]

    peverify cfg "magic.dll"

    peverify cfg "provider.dll"

    peverify cfg "test.exe"

    exec cfg ("." ++ "test.exe") ""
                

[<TestCase("neg1")>]
[<TestCase("neg2")>]
[<TestCase("neg2c")>]
[<TestCase("neg2e")>]
[<TestCase("neg2g")>]
[<TestCase("neg2h")>]    
[<TestCase("neg4")>]
[<TestCase("neg6")>]
[<TestCase("InvalidInvokerExpression")>]
[<TestCase("providerAttributeErrorConsume")>]
[<TestCase("ProviderAttribute_EmptyConsume")>]
[<TestCase("EVIL_PROVIDER_GetNestedNamespaces_Exception")>]
[<TestCase("EVIL_PROVIDER_NamespaceName_Exception")>]
[<TestCase("EVIL_PROVIDER_NamespaceName_Empty")>]
[<TestCase("EVIL_PROVIDER_GetTypes_Exception")>]
[<TestCase("EVIL_PROVIDER_ResolveTypeName_Exception")>]
[<TestCase("EVIL_PROVIDER_GetNamespaces_Exception")>]
[<TestCase("EVIL_PROVIDER_GetStaticParameters_Exception")>]
[<TestCase("EVIL_PROVIDER_GetInvokerExpression_Exception")>]
[<TestCase("EVIL_PROVIDER_GetTypes_Null")>]
[<TestCase("EVIL_PROVIDER_ResolveTypeName_Null")>]
[<TestCase("EVIL_PROVIDER_GetNamespaces_Null")>]
[<TestCase("EVIL_PROVIDER_GetStaticParameters_Null")>]
[<TestCase("EVIL_PROVIDER_GetInvokerExpression_Null")>]
[<TestCase("EVIL_PROVIDER_DoesNotHaveConstructor")>]
[<TestCase("EVIL_PROVIDER_ConstructorThrows")>]
[<TestCase("EVIL_PROVIDER_ReturnsTypeWithIncorrectNameFromApplyStaticArguments")>]
let ``negative type provider tests`` (name:string) =
    let cfg = testConfig "typeProviders/negTests"
    let dir = cfg.Directory

    if requireENCulture () then

        let fileExists = Commands.fileExists dir >> Option.isSome

        rm cfg "provided.dll"

        fsc cfg "--out:provided.dll -a" [".." ++ "helloWorld" ++ "provided.fs"]

        rm cfg "providedJ.dll"

        fsc cfg "--out:providedJ.dll -a" [".." ++ "helloWorld" ++ "providedJ.fs"]

        rm cfg "providedK.dll"

        fsc cfg "--out:providedK.dll -a" [".." ++ "helloWorld" ++ "providedK.fs"]

        rm cfg "provider.dll"

        fsc cfg "--out:provider.dll -a" ["provider.fsx"]

        fsc cfg "--out:provider_providerAttributeErrorConsume.dll -a" ["providerAttributeError.fsx"]

        fsc cfg "--out:provider_ProviderAttribute_EmptyConsume.dll -a" ["providerAttribute_Empty.fsx"]

        rm cfg "helloWorldProvider.dll"

        fsc cfg "--out:helloWorldProvider.dll -a" [".." ++ "helloWorld" ++ "provider.fsx"]

        rm cfg "MostBasicProvider.dll"

        fsc cfg "--out:MostBasicProvider.dll -a" ["MostBasicProvider.fsx"]

        let preprocess name pref = 
            let dirp = (dir |> Commands.pathAddBackslash)
            do
            File.ReadAllText(sprintf "%s%s.%sbslpp" dirp name pref)
                .Replace("<ASSEMBLY>", getfullpath cfg (sprintf "provider_%s.dll" name))
                .Replace("<URIPATH>",sprintf "file:///%s" dirp)
                |> fun txt -> File.WriteAllText(sprintf "%s%s.%sbsl" dirp name pref,txt)

        if name = "ProviderAttribute_EmptyConsume" || name = "providerAttributeErrorConsume" then ()
        else fsc cfg "--define:%s --out:provider_%s.dll -a" name name ["provider.fsx"]

        if fileExists (sprintf "%s.bslpp" name) then preprocess name "" 

        if fileExists (sprintf "%s.vsbslpp" name) then preprocess name "vs"

        SingleTest.singleNegTest cfg name

[<Test>]
let splitAssembly () = 
    let cfg = testConfig "typeProviders/splitAssembly"

    fsc cfg "--out:provider.dll -a" ["provider.fs"]

    fsc cfg "--out:providerDesigner.dll -a" ["providerDesigner.fsx"]

    SingleTest.singleTestBuildAndRunAux cfg FSC_BASIC
        
[<Test>]
let wedgeAssembly () = 
    let cfg = testConfig "typeProviders/wedgeAssembly"

    rm cfg "provider.dll"

    rm cfg "provided.dll"

    fsc cfg "%s" "--out:provided.dll -a" [".." ++ "helloWorld" ++ "provided.fs"]

    rm cfg "providedJ.dll"

    fsc cfg "%s" "--out:providedJ.dll -a" [".." ++ "helloWorld" ++ "providedJ.fs"]

    rm cfg "providedK.dll"

    fsc cfg "%s" "--out:providedK.dll -a" [".." ++ "helloWorld" ++ "providedK.fs"]

    fsc cfg "%s" "--out:provider.dll -a" [".." ++ "helloWorld" ++ "provider.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2a.dll -a" cfg.fsc_flags ["test2a.fs"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2b.dll -a" cfg.fsc_flags ["test2b.fs"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test3.exe" cfg.fsc_flags ["test3.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2a-with-sig.dll -a" cfg.fsc_flags ["test2a.fsi"; "test2a.fs"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2b-with-sig.dll -a" cfg.fsc_flags ["test2b.fsi"; "test2b.fs"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test3-with-sig.exe --define:SIGS" cfg.fsc_flags ["test3.fsx"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2a-with-sig-restricted.dll -a" cfg.fsc_flags ["test2a-restricted.fsi"; "test2a.fs"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test2b-with-sig-restricted.dll -a"cfg.fsc_flags ["test2b-restricted.fsi"; "test2b.fs"]

    fsc cfg "%s --debug+ -r:provider.dll --optimize- -o:test3-with-sig-restricted.exe --define:SIGS_RESTRICTED" cfg.fsc_flags ["test3.fsx"]

    peverify cfg "test2a.dll"

    peverify cfg "test2b.dll"

    peverify cfg "test3.exe"

    exec cfg ("." ++ "test3.exe") ""

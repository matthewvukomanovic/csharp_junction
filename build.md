```
mkdir lib
```

To build the dll in a deterministic way
```
"C:\Program Files (x86)\MSBuild\14.0\Bin\csc" -nologo -optimize -deterministic -pathmap:"%CD"=\publish -t:library -out:lib\Junction.dll Junction.cs
```

To build the dll without optimisations
```
"C:\Program Files (x86)\MSBuild\14.0\Bin\csc" -nologo -t:library -out:lib\Junction.dll Junction.cs
```

To build the unsafe dll (testing)

```
"C:\Program Files (x86)\MSBuild\14.0\Bin\csc" -nologo -t:library /unsafe Junction.Unsafe.cs
"C:\Program Files (x86)\MSBuild\14.0\Bin\csc" -nologo -t:library /unsafe Junction.Unsafe.cs -out:lib\Junction.Unsafe.dll
```

To build the unsafe testing exe (testing)

```
"C:\Program Files (x86)\MSBuild\14.0\Bin\csc" -nologo /unsafe Junction.Unsafe.Testing.cs Junction.Unsafe.cs
"C:\Program Files (x86)\MSBuild\14.0\Bin\csc" -nologo /unsafe Junction.Unsafe.Testing.cs Junction.Unsafe.cs -out:Junction.Unsafe.Testing.exe
```

# PoE-Bot Style Guide

## Function/Method Declarations

All functions and methods names should start with an action verb, except in the case of Builder/Factory class methods.

### Asynchronous/Synchronous Functions

The vast majority of functions and methods within PoE-Bot interact with a remote server or service and are, therfore, asynchronous in nature. It is preferred that, for the purposes of this project, all synchronous functions be suffixed with `Sync`. While any sync function may use any return value, `async` functions should whenever possible return `Task<T>`

**Asynchronous Function Example**
```
public async function DoStuff() 
{

}
```

**Synchronous Function Example**
```
public function <T> DoStuffSync()
{

}
```

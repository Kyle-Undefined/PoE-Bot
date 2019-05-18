# PoE-Bot Style Guide

## Naming Conventions

Naming is one of the most important aspects of code, because it allows multiple collaborators to understand what is going on with the code without actually running it. Proper naming lowers the barrier of entry for new coders and contributors, and can remind veterans of the project what is happening in functions and procedures that have not been recently maintained or modified by the individual. 

### Local Variables

* Be descriptive, yet concise. The name should not only indicate what type of data it is, but how it is *generally* used.
* Local Variables should be `camelCase` without `txtHungarianNotation`. Example: `fieldId` **not** `lFieldId`
* Local Variables should start with a noun, not a descriptor. Example: `fieldId` **not** `idField`

### Local Functions/Method Declarations

All functions and methods names should start with an action verb, except in the case of Builder/Factory class methods.

#### Asynchronous/Synchronous Functions

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

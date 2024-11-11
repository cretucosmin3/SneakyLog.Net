## Features

### Methods without any params or without any loggable params

```json
- Example.Method (2.26ms) >> true
- Example.Method (2.26ms) (..) >> true
```


### Methods with only one primite as a param or return.
Not mentioning the param name as it's obvious if you go to the source.

``` json
- Example.Method (2.26ms) (123) >> true
```

### Methods with one param and one object without any loggable properties.
Same as before, only printing the param value and `{...}` to indicate that there was an object returned or `null`.

``` json
- Example.Method (2.26ms) (123) >> {...}
- Example.Method (2.26ms) (123) >> null
```

### Methods with one object as param or return, logging them on separate lines.
Having these on one line would make it quite long and hard to read.

``` json
- Example.Method (2.26ms)
    >> 123
    << {"id":123,"email":"john@example.com"}
```

### Methods with more loggable params (primitives only)

```json
- Example.Method (2.26ms)
    >> personId: 123, includeEmail: true
    << {"id":123,"email":"john@example.com"}
```

### Methods with out params

```json
- Example.Method (2.26ms)
    >> personId: 123, [out result]: {...}
    << true
```

---

### Example of few of these features combined

```json
- Example.Endpoint (7.89ms)
    - Example.Method (4.23ms) (123) >> {...}
      - Example.Method (0.01ms) >> true
      - Example.Method (0.00ms) (..) >> {...}
      - Example.Method (2.26ms)
          >> personId: 123, includeEmail: true
          << {"id":123,"email":"john@example.com"}
    - Example.Method (3.50ms)
      - Example.Method (3.46ms)
        - Example.Method (1.10ms) () >> {...}
        - Example.Method (1.16ms)
            << {"id":123,"email":"john@example.com"}
        - Example.Method (1.13ms)
            >> 123
            << {"id":123,"email":"john@example.com"}
        - Example.Method (0.04ms) ❌ B3Service.cs:37 - Exception: Just testing...
        - Example.Method (1.08ms) () >> {...}
```
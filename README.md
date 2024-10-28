# SneakyLog.Net

SneakyLog is going to be a library that does logging for you automagically.

### The concept
In some cases logging is tideous to maintain, write. In other cases logging is done too much and it can have an impact on costs or performance.

Sometimes just logging some bit of information is not enough to identify the issue, you need a bigger part of the context in which the code failed.

So... The idea is simple, SneakyLog intercepts and logs the interfaces you have in the code and keeps track of each call, times and data that was passed around so that when you get an error you end up with a fancy print that allows you to go back and understand the issue.

The format of the error trace:

``` Text
- Method A(data) (20ms)
    - Method B(data) (15ms)
    - Method C(data) (15ms)
        - Method C1(data) (15.00ms)
        - Method C2(data):34 ERROR: Error message
```

"What if my method is in a loop?"

``` Text
- Method A(data) (20ms)
    - Method B(data) (15ms)
    - Method C(data) (15ms)
        - Method C1(data) (15.00ms)
        - Method C2 called 68x times (15ms avg)
        - Method C2(data):34 ERROR: Error message
```

---

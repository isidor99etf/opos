# CustomTaskScheduler API

## Klasa UserTask

Ova klasa predstavlja zadatak koji treba da se izvršava.

```c#
public delegate void Job(String message);

public int TaskId;
public Job ThreadJob;
public int CurrentThreadId;
public int Duration;
public int ExecutingTime;
public int Priority;
public bool IsCompleted;
public List<Resource> Resources;
```

Ova klasa ima jedan konstruktor.
Korisnik API-ja nema potrebe da se bavi ovom klasom, nju koristi samo klasa `CustomTaskScheduler`

```C#
public UserTask(
    Job job,
    int priority,
    int duration,
    List<Resource> resources
)
```

## Javni podaci članovi klase

- NumberOfThreads
  - predstavlja broj niti na kojima se izvršavaju zadaci
- MaxDuration
  - maksimalno vrijeme izvršavanja zadatka, primjenjuje se ako se za neku funkciju ne navede koliko je
- IsPreemptive
  - određuje da li je raspoređivanje sa preuzimanjem ili nije
- UseSystemTaskScheduler
  - određuje da li će za raspoređivanje da se koristi privatna klasa `ImprovedSystemTaskScheduler` klase `CustomTaskScheduler` koja nasleđuje `System.Threading.Tasks.TaskScheduler`

## Konstruktor

Dostupan je jedan konstruktor

```c#
public CustomTaskScheduler(
    int numberOfThreads,
    int maxDuration,
    bool isPreemptive,
    bool useSystemTaskScheduler
)
```

## Javne metode

Metoda kojom korisnik prosleđuje metodu za raspoređivanje ima četiri oblika

```c#
public void Schedule(UserTask.Job job, int priority)
public void Schedule(UserTask.Job job, int priority, int duration)
public void Schedule(UserTask.Job job, int priority, List<Resource> resources)
public void Schedule(UserTask.Job job, int priority, int duration, List<Resource> resources)
```

Zadatak koji korisnik šalje na izvršavanje treba da bude metoda koja prima jedan argument tipa `String`, a ne vraća ništa, odnosno povrtani tip je `void`.

Što se resursa tiče, postoji nekoliko resursa definisanih kao statički niz u klasi `CustomTaskScheduler`. Ako korisnik ne proslijedi listu resursa u metodi `Schedule` onda će da se nasumično zadatku dodijele resursi (od 0 do 3 resursa, simulacije radi). Korisniku je ostavljena mogućnost da proslijedi listu resursa (preporučuje se da se koriste resursi definisani u klasi `CustomTaskScheduler`)

Sama klasa `CustomTaskScheduler` ne nasleđuje `System.Threading.Tasks.TaskScheduler`, već njena unutrašnja klasa nasleđuje. Kada korisnik izabere da hoće da koristi "sistemski raspoređivač" (pomoću argumenta u konstruktoru klase `CustomTaskScheduler`) onda se pravi objekat tipa `TaskFactory` kom se u konstruktor prosleđuje objekat tipa `ImprovedSystemTaskScheduler` koji nasleđuje `System.Threading.Tasks.TaskScheduler`.

Korisnik u metodi koja se prosleđuje raspoređivaču prima String koji može da ispiše. Taj isti `String` se ispisuje u `log.txt` fajl čija se putanja ispiše korisniku pri pokretanju raspoređivača. Preporuka je da se ovaj primljeni `String` ne ispisuje na konzolu zbog unosa koji treba da se obavlja. U fajl `log.txt` se takođe ispisuje poruka ako je došlo do promjene prioriteta nekog zadatka (PIP) ili ako je došlo do preuzimanja neke niti od strane zadatka sa višim prioritetom ili ako je neki zadatak raspoređen na slobodnu nit.

## Prioriteti

U klasi `CustomTaskScheduler` definisana je javna klasa `Priority` koja sadrži moguće prioritete, pa se prilikom prosleđivanja zadatka raspoređivaču korisniku preporučuje da koristi prioritete definisane u ovoj klasi.

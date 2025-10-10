rwildcard = $(wildcard $1$2) $(foreach d,$(wildcard $1*),$(call rwildcard,$d/,$2))

SRC_DIR = Source
SRC = $(call rwildcard,$(SRC_DIR),*.cs)
TARGET = bin/Debug/net9.0/MCC

FILE ?= Programs/return_2.c
ASM_FILE = $(FILE:.c=.S)
LIB_FILE = $(FILE:.c=.o)
EXE_FILE = $(basename $(FILE))

.PHONY: all
all: $(TARGET)

$(TARGET): $(SRC)
	@dotnet build --verbosity detailed	

# Runs the compiler then runs the generated executable
.PHONY: run
run:
	@make exe
	@echo "Running executeable..."
	@$(EXE_FILE) || echo $$?

# Generates a reference assembly file using gcc
.PHONY: ref_asm
ref_asm:
	@gcc -S -O -fno-asynchronous-unwind-tables -fcf-protection=none $(FILE) -o $(ASM_FILE)

# Runs compiler to generate a .S file
.PHONY: asm
asm: $(TARGET)
	$(TARGET) $(FILE) $(ASM_FILE)

.PHONY: exe
exe:
	@make asm
	@gcc $(ASM_FILE) -o $(EXE_FILE)

# Generates .o file
# Use to compile programs without a main function
.PHONY: lib
lib:
	@make asm
	@gcc -c $(ASM_FILE) -o $(LIB_FILE)

# Removes temporary directories
.PHONY: clean
clean:
	@rm -rf bin obj

# Runs test suite
CHAPTER ?= 1
STAGE ?= run
.PHONY: test
test:
	./Modules/writing-a-c-compiler-tests/test_compiler mcc --chapter $(CHAPTER) --stage $(STAGE)

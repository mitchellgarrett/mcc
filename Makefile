rwildcard = $(wildcard $1$2) $(foreach d,$(wildcard $1*),$(call rwildcard,$d/,$2))

SRC_DIR = src
SRC = $(wildcard $(SRC_DIR)/*.cs) $(wildcard $(SRC_DIR)/assembly/*.cs) $(wildcard $(SRC_DIR)/codegen/*.cs) $(wildcard $(SRC_DIR)/compiler/*.cs) $(wildcard $(SRC_DIR)/intermediate/*.cs) 
BUILD_DIR = build
TARGET = $(BUILD_DIR)/mcc.exe

CSC_FLAGS = -errorendlocation

FILE ?= programs/return_2.c
ASM_FILE = $(FILE:.c=.S)
EXE_FILE = $(basename $(FILE))

.PHONY: all
all: $(TARGET)

$(TARGET): $(SRC)
	@mkdir -p $(BUILD_DIR)
	@csc $(SRC) -out:$(TARGET) $(CSC_FLAGS)

.PHONY: run
run:
	@make asm
	@make exe
	@echo "Running executeable..."
	@$(EXE_FILE) || echo $$?

.PHONY: ref_asm
ref_asm:
	@gcc -S -O -fno-asynchronous-unwind-tables -fcf-protection=none $(FILE)

.PHONY: asm
asm: $(TARGET)
	@mono $(TARGET) $(FILE) $(ASM_FILE)

.PHONY: exe
exe:
	@gcc $(ASM_FILE) -o $(EXE_FILE)

.PHONY: clean
clean:
	@rm -rf $(BUILD_DIR)
	@rm -rf bin obj

CHAPTER ?= 1
STAGE ?= run
.PHONY: test
test:
	./deps/writing-a-c-compiler-tests/test_compiler mcc --chapter $(CHAPTER) --stage $(STAGE) --skip-invalid

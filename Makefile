SRC_DIR = src
SRC = $(wildcard $(SRC_DIR)/*.cs)
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
	@./$(EXE_FILE) || echo $$?

.PHONY: ref_asm
ref_asm:
	@gcc -S -O -fno-asynchronous-unwind-tables -fcf-protection=none $(FILE)

.PHONY: asm
asm: $(TARGET)
	@mono $(TARGET) $(FILE) $(ASM_FILE)

.PHONY: exe
exe:
	@gcc $(ASM_FILE) -o $(EXE_FILE)

clean:
	@rm -rf $(BUILD_DIR)

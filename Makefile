SRC_DIR = src
SRC = $(wildcard $(SRC_DIR)/*.cs)
BUILD_DIR = build
TARGET = $(BUILD_DIR)/mcc.exe

CSC_FLAGS = -errorendlocation

FILE ?= test.c

.PHONY: all
all: $(TARGET)

$(TARGET): $(SRC)
	@mkdir -p $(BUILD_DIR)
	@csc $(SRC) -out:$(TARGET) $(CSC_FLAGS)

.PHONY: run
run:
	@mono $(TARGET) $(FILE)

clean:
	@rm -rf $(BUILD_DIR)

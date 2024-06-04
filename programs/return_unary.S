# int main(void)
.globl _main
_main:
	pushq %rbp
	movq %rsp, %rbp

	subq $8, %rsp
	movl $2, -4(%rbp)
	negl -4(%rbp)
	movl -4(%rbp), %eax
	movl %eax, -8(%rbp)
	notl -8(%rbp)
	movl -8(%rbp), %eax
	
	movq %rbp, %rsp
	popq %rbp
	ret

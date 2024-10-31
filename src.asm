	.section .text
	.global	_main
_main:
LFB0:
	push	ebp
	mov	ebp, esp
	and	esp, -16
	sub	esp, 16
	call	___main
	mov	DWORD PTR [esp+12], 0
	mov	eax, 1
	leave
	ret
LFE0:
	.ident	"GCC: (Rev5, Built by MSYS2 project) 13.2.0"

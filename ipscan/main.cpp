#include "hash_scanner.hpp"

#include <Windows.h>

#include <functional>
#include <chrono>
#include <iostream>

#ifndef DllExport
#define DllExport	__declspec(dllexport) __stdcall
#endif

namespace psi {
namespace benchmark {

void timed_event(std::string const& description, std::function<void()> functor)
{
	auto start = std::chrono::high_resolution_clock::now();
	std::cout << description << std::endl;

	try
	{
		functor();
	}
	catch (std::exception const& e)
	{
		std::cout << "Exception: " << e.what() << std::endl;
	}

	auto end = std::chrono::high_resolution_clock::now();
	auto time_span = std::chrono::duration_cast<std::chrono::duration<double>>(end - start);

	std::cout << "Finished in " << time_span.count() << " seconds" << std::endl;
}

} // namespace benchmark

static hash_scanner scanner;

void open_input(char const* filename)
{
	benchmark::timed_event("Opening input file...", [&]() -> void
	{
		scanner.source_add(filename);
	});
}

void clear_input()
{
	benchmark::timed_event("Clearing input data...", [&]() -> void
	{
		scanner.source_clear();
	});
}

void test_hashes(uint8_t* input, int count, SAFEARRAY** output)
{
	std::vector<std::array<uint8_t, 16>> hashes(count);
	std::vector<std::array<uint8_t, 16>> matches;

	for (int i = 0; i < count; i++)
		memcpy(&hashes[i][0], input + (i * 16), 16);

	benchmark::timed_event("Scanning for hashes...", [&]() -> void
	{
		scanner.test(hashes, matches);
	});

	SAFEARRAYBOUND bound;
	bound.lLbound = 0;
	bound.cElements = static_cast<ULONG>(matches.size()) * 16;

	if ((*output = SafeArrayCreate(VT_UI1, 1, &bound)) != nullptr)
	{
		uint8_t* buffer = nullptr;

		if (FAILED(SafeArrayAccessData(*output, reinterpret_cast<void**>(&buffer))))
			std::cout << "Failed to access safe array" << std::endl;
		else
		{
			for (std::size_t i = 0; i < matches.size(); i++)
				memcpy(&buffer[i * 16], matches[i].data(), 16);

			if (FAILED(SafeArrayUnaccessData(*output)))
				std::cout << "Failed to release safe array" << std::endl;
		}
	}
}

} // namespace psi

#ifdef __cplusplus
extern "C" {
#endif

void DllExport AddSource(char const* filename)
{
	psi::open_input(filename);
}

void DllExport ClearSources()
{
	psi::clear_input();
}

void DllExport TestHashes(uint8_t* input, int count, SAFEARRAY** output)
{
	psi::test_hashes(input, count, output);
}

#ifdef __cplusplus
}
#endif

BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpvReserved)
{
	if (dwReason == DLL_PROCESS_ATTACH)
	{
		/* On library being loaded */
	}

	return TRUE;
}
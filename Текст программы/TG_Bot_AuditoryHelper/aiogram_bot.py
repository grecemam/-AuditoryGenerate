from aiogram import Bot, Dispatcher, types
from aiogram.types import Message, CallbackQuery
from aiogram.utils import executor
from aiogram.contrib.fsm_storage.memory import MemoryStorage
from aiogram.dispatcher import FSMContext
from aiogram.dispatcher.filters.state import State, StatesGroup
import aiohttp
import os
import json
import datetime
from PIL import Image, ImageDraw, ImageFont
import re
import io
import difflib
from bs4 import BeautifulSoup
import ssl
import asyncio


API_TOKEN = "7239904904:AAERNwCNjt59pZGFpVWGinLBwNZeNEsZFJo"
API_BASE_URL = "http://auditoryhelperapi.somee.com"

bot = Bot(token=API_TOKEN)
storage = MemoryStorage()
dp = Dispatcher(bot, storage=storage)

class ScheduleState(StatesGroup):
    waiting_for_group = State()
    group_selected = State()

WEEK_DAYS = ["ПОНЕДЕЛЬНИК", "ВТОРНИК", "СРЕДА", "ЧЕТВЕРГ", "ПЯТНИЦА", "СУББОТА"]

DAYS_MAPPING = {
    "Monday": "ПОНЕДЕЛЬНИК",
    "Tuesday": "ВТОРНИК",
    "Wednesday": "СРЕДА",
    "Thursday": "ЧЕТВЕРГ",
    "Friday": "ПЯТНИЦА",
    "Saturday": "СУББОТА",
    "Sunday": "ВОСКРЕСЕНЬЕ"
}

def get_russian_day():
    today_en = datetime.datetime.now().strftime("%A")
    return DAYS_MAPPING.get(today_en, today_en)

def create_days_keyboard(group_schedule: dict) -> types.InlineKeyboardMarkup:
    keyboard = types.InlineKeyboardMarkup(row_width=3)
    available_days = []

    for day in WEEK_DAYS:
        day_data = group_schedule.get("Days", {}).get(day)
        if day_data:
            has_lessons = any(day_data.get("Buildings", {}).values())
            if has_lessons:
                available_days.append(day)

    buttons = [
        types.InlineKeyboardButton(day.title(), callback_data=f"day_{day}")
        for day in available_days
    ]
    keyboard.add(*buttons)
    return keyboard

@dp.message_handler(commands=["start"])
async def cmd_start(message: Message):
    text = (
        "👋 Привет! Я — *AuditoryBot*, твой помощник по расписанию и аудиториям.\n\n"
        "📌 Вот что я умею:\n"
        "• /schedule — узнать расписание по группе\n"
        "• /auditory — получить файл с распределением аудиторий\n"
        "• /teacher Фамилия — узнать, где преподаёт указанный преподаватель\n\n"
        "🧭 Просто введи нужную команду или нажми на кнопку меню. Если что — я всегда рядом 😎"
    )
    await message.answer(text, parse_mode="Markdown")


@dp.message_handler(commands=["schedule"], state="*")
async def cmd_schedule(message: Message, state: FSMContext):
    await state.finish()  # сбрасываем состояние, если вдруг застряли
    await message.answer("📅 Введите название вашей группы, например: П50-7-21")
    await ScheduleState.waiting_for_group.set()
@dp.message_handler(state=ScheduleState.waiting_for_group)
async def handle_group_name(message: Message, state: FSMContext):
    if message.text.startswith("/"):
        await state.finish()
        await bot.process_updates([types.Update(message=message)])
        return

    group_name = message.text.strip()

    try:
        with open("Расписание_МПТ.json", "r", encoding="utf-8") as f:
            schedule_data = json.load(f)
    except Exception as e:
        await message.answer(f"❌ Не удалось загрузить файл расписания: {e}")
        return

    all_groups = schedule_data.get("Groups", {})
    matched_name = next((g for g in all_groups.keys() if group_name.lower() == g.lower()), None)

    if matched_name:
        await state.update_data(group_name=matched_name)
        await message.answer(f"✅ Группа найдена: {matched_name}\n⏳ Загружаю расписание...")
        await send_schedule_for_today(message, matched_name)
        await ScheduleState.group_selected.set()
    else:
        await message.answer("❌ Группа не найдена. Проверьте название.")


async def send_schedule_for_today(message_or_query, group_name, day=None):
    try:
        with open("Расписание_МПТ.json", "r", encoding="utf-8") as f:
            schedule_data = json.load(f)
    except Exception as e:
        await message_or_query.answer(f"❌ Не удалось загрузить файл расписания: {e}")
        return

    today = day or get_russian_day()
    group_schedule = schedule_data.get("Groups", {}).get(group_name)

    if not group_schedule:
        await message_or_query.answer("❌ Расписание для этой группы не найдено.")
        return

    day_schedule = group_schedule.get("Days", {}).get(today.upper())
    if not day_schedule:
        await message_or_query.answer(f"ℹ️ На {today.title()} занятий нет.")
        return

    result = f"📚 *Расписание для группы {group_name} на {today.title()}*\n\n"

    number_emojis = {
        "1": "1 пара: ", "2": "2 пара: ", "3": "3 пара: ",
        "4": "4 пара: ", "5": "5 пара: ", "6": "6 пара: ",
        "7": "7 пара: ", "8": "8 пара: "
    }

    for building, lessons in day_schedule.get("Buildings", {}).items():
        result += f"🏛️ *{building}*\n"
        if not lessons:
            result += "_Нет занятий_\n\n"
            continue
        for lesson in lessons:
            pair_raw = lesson.get("LessonNumber", "")
            subject = lesson.get("Subject", "Без названия")
            teacher = lesson.get("Teacher", "Не указан")

            match = re.match(r"(\d+)", pair_raw.strip())
            if match:
                num = match.group(1)
                emoji = number_emojis.get(num, f"{num})")
            else:
                emoji = "🔹"

            result += f"{emoji} {subject} — {teacher}\n"
        result += "\n"
    # Подгружаем и вставляем замены
    changes_text = await get_schedule_changes_for_day(group_name, today)
    if changes_text:
        result += "\n\n" + changes_text

    if isinstance(message_or_query, Message):
        await message_or_query.answer(result, parse_mode="Markdown", reply_markup=create_days_keyboard(group_schedule))
    elif isinstance(message_or_query, CallbackQuery):
        await message_or_query.message.answer(result, parse_mode="Markdown", reply_markup=create_days_keyboard(group_schedule))
    

@dp.callback_query_handler(lambda c: c.data.startswith("day_"), state=ScheduleState.group_selected)
async def handle_day_callback(callback_query: CallbackQuery, state: FSMContext):
    selected_day = callback_query.data.replace("day_", "").upper()
    data = await state.get_data()
    group_name = data.get("group_name")
    try:
        await callback_query.message.delete()
    except Exception as e:
        print(f"Не удалось удалить сообщение: {e}")
    await send_schedule_for_today(callback_query, group_name, day=selected_day)
    await callback_query.answer()

@dp.message_handler(commands=["auditory"], state="*")
async def cmd_auditory(message: Message, state: FSMContext):
    await state.finish()  # сбрасываем состояние
    keyboard = types.InlineKeyboardMarkup()
    keyboard.add(
        types.InlineKeyboardButton("Нахимовский", callback_data="auditory_nahim"),
        types.InlineKeyboardButton("Нежинская", callback_data="auditory_nezhka")
    )
    await message.answer("🏢 Выберите корпус:", reply_markup=keyboard)

@dp.callback_query_handler(lambda c: c.data.startswith("auditory_"))
async def send_auditory_file(callback_query: CallbackQuery):
    campus = callback_query.data.replace("auditory_", "")
    filename = "AuditFileNahimov.xlsx" if campus == "nahim" else "AuditFileNezhka.xlsx"
    path = f"{API_BASE_URL}/files/{filename}"

    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(path) as resp:
                if resp.status == 200:
                    data = await resp.read()
                    buffer = io.BytesIO(data)
                    buffer.name = filename
                    await bot.send_document(callback_query.message.chat.id, types.InputFile(buffer))
                else:
                    await callback_query.message.answer("⚠️ Не удалось получить файл с сервера.")
    except Exception as e:
        await callback_query.message.answer(f"Ошибка загрузки файла: {e}")
    await callback_query.answer()

@dp.message_handler(commands=["teacher"], state="*")
async def cmd_teacher(message: Message, state: FSMContext):
    await state.finish()
    teacher_query = message.text.replace("/teacher", "").strip().lower()

    if not teacher_query:
        await message.answer("✍️ Укажите фамилию преподавателя. Пример:\n`/teacher Фамилия`", parse_mode="Markdown")
        return

    async with aiohttp.ClientSession() as session:
        try:
            # Загружаем распределение преподавателей
            async with session.get(f"{API_BASE_URL}/files/distributed_teachers_today.json") as resp1:
                if resp1.status != 200:
                    await message.answer("⚠️ Не удалось получить данные распределения преподавателей.")
                    return
                raw_data = await resp1.read()
                decoded = raw_data.decode("utf-8-sig")
                teacher_list = json.loads(decoded)

            # Загружаем всех преподавателей
            async with session.get(f"{API_BASE_URL}/api/Teachers") as resp2:
                if resp2.status != 200:
                    await message.answer("⚠️ Не удалось получить список всех преподавателей.")
                    return
                all_teachers = await resp2.json()

            reply = ""
            names = [t["fullName"] for t in all_teachers]
            closest = difflib.get_close_matches(teacher_query, [n.lower() for n in names], n=1, cutoff=0.6)
            matched = [t for t in all_teachers if t["fullName"].lower() in closest]


            if not matched:
                await message.answer("❌ Преподаватель не найден.")
                return

            for t in matched:
                full_name = t["fullName"]
                found = next((d for d in teacher_list if full_name.lower() in d["teacher"].lower()), None)
                if found:
                    reply += f"👨‍🏫 *{full_name}*\n🏢 Корпус: *{found['campus']}*\n🏫 Кабинет: *{found['room']}*\n\n"
                else:
                    reply += f"👨‍🏫 *{full_name}*\n📅 *Выходной :)*\n\n"

            await message.answer(reply.strip(), parse_mode="Markdown")

        except Exception as e:
            await message.answer(f"🚫 Ошибка: {e}")

async def get_schedule_changes_for_day(group_name: str, day_rus: str) -> str:
    url = "https://mpt.ru/izmeneniya-v-raspisanii/"
    ssl_context = ssl.create_default_context()
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE

    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(url, ssl=ssl_context) as response:
                if response.status != 200:
                    return ""

                html = await response.text()
                soup = BeautifulSoup(html, "html.parser")

                # 1. Получаем дату из заголовка "Замены на 05.05.2025"
                h4 = soup.find("h4")
                if not h4 or "замены на" not in h4.text.lower():
                    return ""

                match = re.search(r"(\d{2}\.\d{2}\.\d{4})", h4.text)
                if not match:
                    return ""

                target_date = match.group(1)  # например "05.05.2025"

                tables = soup.find_all("table", class_="table-striped")
                for table in tables:
                    caption = table.find("caption")
                    if caption and group_name.lower() in caption.text.lower():
                        rows = table.find_all("tr")[1:]
                        changes = []
                        for row in rows:
                            cols = row.find_all("td")
                            if len(cols) >= 3:
                                para = cols[0].text.strip()
                                old = cols[1].text.strip()
                                new = cols[2].text.strip()
                                changes.append(f"{para} пара: {old} ➜ {new}")
                        if changes:
                            return f"🔄 *Изменения на {target_date}*:\n" + "\n".join(changes)
        return ""
    except Exception as e:
        return f"⚠️ Ошибка при получении замен: {e}"

def get_next_date_for_weekday(russian_weekday: str) -> str:
    day_to_index = {
        "ПОНЕДЕЛЬНИК": 0,
        "ВТОРНИК": 1,
        "СРЕДА": 2,
        "ЧЕТВЕРГ": 3,
        "ПЯТНИЦА": 4,
        "СУББОТА": 5,
        "ВОСКРЕСЕНЬЕ": 6
    }

    today = datetime.datetime.now()
    today_idx = today.weekday()
    target_idx = day_to_index.get(russian_weekday.upper(), 0)

    days_ahead = (target_idx - today_idx + 7) % 7
    if days_ahead == 0:
        days_ahead = 7  # Следующий понедельник, если сегодня понедельник

    target_date = today + datetime.timedelta(days=days_ahead)
    return target_date.strftime("%d.%m.%Y")


if __name__ == "__main__":
    executor.start_polling(dp, skip_updates=True)
